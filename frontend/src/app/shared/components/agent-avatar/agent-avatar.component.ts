import { Component, computed, input } from '@angular/core';

const COLORS = [
  '#4f46e5', '#0891b2', '#059669', '#d97706',
  '#dc2626', '#7c3aed', '#db2777', '#ea580c',
];

function hashName(name: string): number {
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) & 0xffff;
  return h;
}

/**
 * Circular initials avatar with an optional verified badge.
 *
 * Usage:
 *   <app-agent-avatar name="Mona Ibrahim" [verified]="true" size="lg" />
 *
 * Sizes: 'sm' (32px) | 'md' (44px, default) | 'lg' (60px)
 */
@Component({
  selector: 'app-agent-avatar',
  standalone: true,
  imports: [],
  template: `
    <div class="aa-wrap" [class]="'aa-' + size()">
      <div class="aa-circle" [style.background]="bgColor()">
        {{ initials() }}
      </div>
      @if (verified()) {
        <span class="aa-verified" title="Verified">
          <i class="fa-solid fa-check"></i>
        </span>
      }
    </div>
  `,
  styles: [`
    :host { display: inline-flex; }

    .aa-wrap {
      position: relative;
      display: inline-flex;
      flex-shrink: 0;
    }

    .aa-circle {
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      font-weight: 800;
      letter-spacing: -.3px;
      user-select: none;
      font-family: inherit;
    }

    /* Size variants */
    .aa-sm .aa-circle { width: 32px; height: 32px; font-size: 11px; }
    .aa-md .aa-circle { width: 44px; height: 44px; font-size: 15px; }
    .aa-lg .aa-circle { width: 60px; height: 60px; font-size: 20px; }

    /* Verified badge */
    .aa-verified {
      position: absolute;
      bottom: 0;
      right: 0;
      background: #2563eb;
      color: #fff;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      border: 2px solid #fff;
    }
    .aa-sm .aa-verified { width: 14px; height: 14px; font-size: 7px; }
    .aa-md .aa-verified { width: 17px; height: 17px; font-size: 8px; }
    .aa-lg .aa-verified { width: 22px; height: 22px; font-size: 10px; }
  `],
})
export class AgentAvatarComponent {
  name     = input.required<string>();
  verified = input<boolean>(false);
  size     = input<'sm' | 'md' | 'lg'>('md');

  protected initials = computed(() => {
    const parts = this.name().trim().split(/\s+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return this.name().trim().slice(0, 2).toUpperCase();
  });

  protected bgColor = computed(() =>
    COLORS[hashName(this.name()) % COLORS.length]
  );
}
