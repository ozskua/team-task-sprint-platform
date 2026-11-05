# Event contracts

> The three services deliberately share no assembly, so each one declares its own copy of the
> types below. When two copies disagree, this document decides which is right.

Everything travels as **UTF-8 JSON** with `content_type: application/json`, `delivery_mode: 2`
(persistent) and `message_id` set to the event's `EventId`. Field names are PascalCase — the .NET
default, with no serializer configuration on either side.

## Topology

```
exchange sprintflow.notifications   (direct, durable)
  └── notification.created ──► queue sprintflow.notifications (durable)
                                  └── consumer: SprintFlow.Web.Notification

exchange sprintflow.realtime        (direct, durable)
  ├── notification.delivered ─┐
  ├── board.changed ──────────┴──► queue sprintflow.realtime (durable)
                                      └── consumer: SprintFlow.Web.SignalR
```

Separate queues are required, not cosmetic: two consumers on one queue would **split** the messages
between them, so roughly half would be persisted and the other half delivered.

## `notification.created`

Published by **SprintFlow.Web.Server** after the domain change is committed.
Consumed by **SprintFlow.Web.Notification**.

| Field | Type | Meaning |
|---|---|---|
| `EventId` | GUID | Identifies this occurrence. Delivery is at-least-once; nothing checks it yet |
| `Type` | int | 1 `TaskAssigned` · 2 `TaskCommented` · 3 `TaskCompleted` · 4 `TaskCancelled` |
| `TaskId` | int | The task the notification is about |
| `ActorUserId` | int | Who did the thing. Already excluded from the recipients |
| `RecipientUserIds` | int[] | Who to notify — **decided at event time**, deduplicated, active users only |
| `CreatedAt` | UTC date-time | When the domain change happened, not when it was delivered |

```json
{
  "EventId": "f5612e5b-5268-4ac7-8755-69f0ff045993",
  "Type": 2,
  "TaskId": 1143,
  "ActorUserId": 3,
  "RecipientUserIds": [4, 5],
  "CreatedAt": "2026-09-09T13:55:41.7312000Z"
}
```

**The recipients travel with the event on purpose.** Team membership is computed from
`TaskParticipants` and can change between publish and consume; resolving it downstream would
describe the board as it is at delivery time rather than when the thing happened.

## `notification.delivered`

Published by **SprintFlow.Web.Notification** once the rows are durable.
Consumed by **SprintFlow.Web.SignalR**.

| Field | Type | Meaning |
|---|---|---|
| `EventId` | GUID | Carried through from the originating `notification.created` |
| `RecipientUserIds` | int[] | Who to reach over SignalR |

```json
{ "EventId": "f5612e5b-5268-4ac7-8755-69f0ff045993", "RecipientUserIds": [4, 5] }
```

Only what delivery needs. The notification's type, task and text are read back over REST by the
client, so none of them appear here.

## `board.changed`

Published by **SprintFlow.Web.Server** after a board-affecting write.
Consumed by **SprintFlow.Web.SignalR**.

| Field | Type | Meaning |
|---|---|---|
| `EventId` | GUID | Identifies this occurrence |
| `BoardId` | int | Which board changed |
| `Reason` | string | `taskCreated` · `taskUpdated` · `taskMoved` · `taskDeleted` · `taskSprintChanged` |

```json
{ "EventId": "a1c0f0de-8b21-4f1e-9a44-2c7d5b6e1f90", "BoardId": 19, "Reason": "taskMoved" }
```

The reason strings are **the ones the browser already receives** and must not be renamed: the client
listens for `boardChanged` and re-reads the board.

## What reaches the browser

Both hubs send announcements, never data — REST stays the source of truth.

| Hub | Event | Payload | Addressed to |
|---|---|---|---|
| `NotificationHub` | `notificationReceived` | none | `Clients.Users(ids)` — by the `NameIdentifier` claim |
| `BoardHub` | `boardChanged` | `{ boardId, reason }` | `Clients.Group("board:{id}")` |

## Acknowledgement rules

**Notification service** — persistence is the commitment, announcing is best effort:

```
unreadable payload / no recipients → nack(requeue: false) or ack   — never retried
SQL insert fails                   → nack(requeue: true)           — worth retrying
SQL insert succeeds                → ACK immediately
then announce                      → failure logs a warning only   — never requeued
```

Acknowledging *before* announcing is what prevents a failed announcement replaying the insert and
duplicating notifications.

**Realtime service** — delivery is best effort by nature:

```
unreadable payload / unknown routing key → nack(requeue: false)
delivery attempted                       → ACK
```

There is no requeue: retrying does not make an absent browser present, and it would block the queue.
A missed live update is recovered by the client reading over REST.

## Known gaps

No outbox (a broker outage at publish time loses the event), no idempotency check (`EventId` is
carried but nothing consumes it, so at-least-once delivery can duplicate rows), no dead-letter queue
(an unreadable message is discarded with an error log), and no retry backoff.

---

# Outbound webhooks

Unlike the events above, these leave the system: the Server sends them **over HTTPS to a URL the
administrator stored on an Area** (`Areas.WebhookUrl`). The receiver is the
customer's own API; nothing in this repository receives them. One event exists.

## `sprint.report.created`

**When:** the administrator exports the sprint report (`GET /api/admin/sprint-report/export/xlsx`
or `/pdf`). For every Area whose `WebhookUrl` is set, one request is sent, carrying **that Area's
sprints only**, rendered in the same format the administrator downloaded. Sent synchronously inside
the export request; **one attempt, 10 s timeout, no retry**; a failure never fails the export.

**Request**

```
POST {Areas.WebhookUrl}
Content-Type: multipart/form-data; boundary=…
X-SprintFlow-Event:     sprint.report.created
X-SprintFlow-Event-Id:  <Guid, unique per Area per export>
X-SprintFlow-Timestamp: <Unix seconds, UTC>
X-SprintFlow-Signature: sha256=<hex HMAC-SHA256>
```

Two form parts, in this order:

| name | Content-Type | content |
|---|---|---|
| `metadata` | `application/json; charset=utf-8` | the JSON object below |
| `file` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` or `application/pdf` | the report bytes; `filename` = `report.fileName` |

**`metadata`** — camelCase (`JsonSerializerDefaults.Web`), the one place this project departs from
the PascalCase of the broker events, because the receiver is not a .NET service of ours:

```json
{
  "eventId": "3b1e5f0e-2c1a-4b7c-9e7d-8f1c2a3b4c5d",
  "event": "sprint.report.created",
  "occurredAt": "2026-09-17T15:25:12.1234567Z",
  "areaId": 20,
  "areaName": "KargoPark",
  "report": {
    "format": "xlsx",
    "fileName": "SprintReport_2026-09-17_20.xlsx",
    "sizeBytes": 9854,
    "sha256": "<hex SHA-256 of the file part>",
    "contentType": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
  },
  "summary": { "sprints": 6, "tasks": 21, "completed": 2, "cancelled": 0, "ongoing": 19 },
  "producedByUserId": 1
}
```

`format` is `xlsx` or `pdf`. `summary` counts the Area's sprints exactly as the report's own total
row does. No user names, e-mail addresses or task titles are in the metadata; the report itself is
the file.

**Signature.** With `secret` = the shared `Webhooks:Secret` (UTF-8):

```
message   = timestamp + "." + eventId + "." + lowercase-hex-sha256(file bytes) + "." + metadata JSON (exact bytes of the part)
signature = lowercase-hex( HMAC-SHA256(secret, UTF-8(message)) )
header    = "sha256=" + signature
```

The file's own hash is inside the signed text, so a replaced file fails verification even with a
valid header.

**What the receiver must do**

1. Recompute the signature from the raw parts and compare it in constant time; reject on mismatch.
2. Reject a `X-SprintFlow-Timestamp` outside its replay window (five minutes is the intended value).
3. Treat `X-SprintFlow-Event-Id` as idempotency key: the same id delivered twice is the same report.
4. Store or process the file, then answer any **2xx** with an empty body; the Server ignores the
   body. Any other status, a timeout, or a refused connection is logged at Warning on the Server
   side as a failed delivery for that Area and is **not** retried.

**Where the outcome is visible.** The export response carries
`X-SprintFlow-Webhook: delivered=<n>;failed=<n>;skipped=<n>` (skipped = Areas without a URL), and
the Server log has one Information line per delivery and one Warning per failure naming the Area,
the target host, the status or reason and the event id — never the secret, the signature or the
response body.
