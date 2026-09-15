import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { TrendPoint } from '../dashboard-models';

@Component({
  selector: 'app-trend-chart',
  templateUrl: './trend-chart.html',
  standalone: false,
  styleUrl: './trend-chart.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrendChart {
  readonly points = input.required<readonly TrendPoint[]>();
  readonly loading = input(false);

  protected readonly viewWidth = 720;
  protected readonly viewHeight = 240;
  private readonly padding = { top: 16, right: 16, bottom: 28, left: 40 };

  protected readonly hoveredIndex = signal<number | null>(null);

  protected readonly hasData = computed(() => this.points().length > 0);

  private readonly maxValue = computed(() => {
    const values = this.points().flatMap(point => [point.created, point.completed]);
    return Math.max(1, ...values);
  });

  protected readonly createdLine = computed(() => this.buildLine(point => point.created));
  protected readonly completedLine = computed(() => this.buildLine(point => point.completed));

  protected readonly yTicks = computed(() => {
    const steps = 4;
    const max = this.maxValue();

    return Array.from({ length: steps + 1 }, (_, index) => {
      const value = Math.round((max / steps) * index);
      return { value, y: this.yFor(value) };
    });
  });

  protected readonly xLabels = computed(() => {
    const points = this.points();
    if (points.length === 0) {
      return [];
    }

    const step = Math.max(1, Math.ceil(points.length / 5));
    const labels: { x: number; text: string }[] = [];

    for (let index = 0; index < points.length; index += step) {
      labels.push({ x: this.xFor(index), text: this.formatDate(points[index].date) });
    }

    return labels;
  });

  protected readonly hoverZones = computed(() => {
    const points = this.points();
    const width = this.plotWidth() / Math.max(1, points.length);

    return points.map((_, index) => ({
      index,
      x: this.padding.left + width * index,
      width
    }));
  });

  protected readonly hovered = computed(() => {
    const index = this.hoveredIndex();
    const points = this.points();

    if (index === null || index >= points.length) {
      return null;
    }

    const point = points[index];

    return {
      point,
      label: this.formatDate(point.date),
      x: this.xFor(index),
      createdY: this.yFor(point.created),
      completedY: this.yFor(point.completed),
      leftPercent: (this.xFor(index) / this.viewWidth) * 100
    };
  });

  protected onLeave(): void {
    this.hoveredIndex.set(null);
  }

  private plotWidth(): number {
    return this.viewWidth - this.padding.left - this.padding.right;
  }

  private plotHeight(): number {
    return this.viewHeight - this.padding.top - this.padding.bottom;
  }

  private xFor(index: number): number {
    const count = this.points().length;
    if (count <= 1) {
      return this.padding.left + this.plotWidth() / 2;
    }
    return this.padding.left + (this.plotWidth() * index) / (count - 1);
  }

  private yFor(value: number): number {
    const ratio = value / this.maxValue();
    return this.padding.top + this.plotHeight() * (1 - ratio);
  }

  private buildLine(pick: (point: TrendPoint) => number): string {
    return this.points()
      .map((point, index) => `${this.xFor(index)},${this.yFor(pick(point))}`)
      .join(' ');
  }

  private formatDate(isoDate: string): string {
    return new Date(isoDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
  }
}
