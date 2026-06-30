import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { InViewDirective } from '../../shared/directives/in-view.directive';

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [RouterLink, InViewDirective, TranslatePipe],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss',
})
export class ServicesComponent {
  // Text fields hold i18n keys resolved with the `translate` pipe in the template.
  readonly services = [
    { icon: 'fa-house-chimney',         color: '#4f46e5', bg: 'rgba(79,70,229,.1)',  title: 'SERVICES.SVC1_TITLE', desc: 'SERVICES.SVC1_DESC' },
    { icon: 'fa-wand-magic-sparkles',   color: '#8b5dee', bg: 'rgba(139,93,238,.1)', title: 'SERVICES.SVC2_TITLE', desc: 'SERVICES.SVC2_DESC' },
    { icon: 'fa-users',                 color: '#0891b2', bg: 'rgba(8,145,178,.1)',  title: 'SERVICES.SVC3_TITLE', desc: 'SERVICES.SVC3_DESC' },
    { icon: 'fa-chart-line',            color: '#059669', bg: 'rgba(5,150,105,.1)',  title: 'SERVICES.SVC4_TITLE', desc: 'SERVICES.SVC4_DESC' },
    { icon: 'fa-screwdriver-wrench',    color: '#d97706', bg: 'rgba(217,119,6,.1)',  title: 'SERVICES.SVC5_TITLE', desc: 'SERVICES.SVC5_DESC' },
    { icon: 'fa-crown',                 color: '#dc2626', bg: 'rgba(220,38,38,.1)',  title: 'SERVICES.SVC6_TITLE', desc: 'SERVICES.SVC6_DESC' },
  ];

  readonly roles = [
    { icon: 'fa-house-chimney', gradient: 'linear-gradient(135deg,#4f46e5,#6366f1)', label: 'SERVICES.ROLE1_LABEL', points: ['SERVICES.ROLE1_PT1', 'SERVICES.ROLE1_PT2', 'SERVICES.ROLE1_PT3', 'SERVICES.ROLE1_PT4'] },
    { icon: 'fa-key',           gradient: 'linear-gradient(135deg,#0891b2,#38bdf8)', label: 'SERVICES.ROLE2_LABEL', points: ['SERVICES.ROLE2_PT1', 'SERVICES.ROLE2_PT2', 'SERVICES.ROLE2_PT3', 'SERVICES.ROLE2_PT4'] },
    { icon: 'fa-handshake',     gradient: 'linear-gradient(135deg,#f59e0b,#fbbf24)', label: 'SERVICES.ROLE3_LABEL', points: ['SERVICES.ROLE3_PT1', 'SERVICES.ROLE3_PT2', 'SERVICES.ROLE3_PT3', 'SERVICES.ROLE3_PT4'] },
    { icon: 'fa-building',      gradient: 'linear-gradient(135deg,#059669,#34d399)', label: 'SERVICES.ROLE4_LABEL', points: ['SERVICES.ROLE4_PT1', 'SERVICES.ROLE4_PT2', 'SERVICES.ROLE4_PT3', 'SERVICES.ROLE4_PT4'] },
  ];

  readonly steps = [
    { num: '01', icon: 'fa-user-plus',           title: 'SERVICES.STEP1_TITLE', desc: 'SERVICES.STEP1_DESC' },
    { num: '02', icon: 'fa-house-circle-check',  title: 'SERVICES.STEP2_TITLE', desc: 'SERVICES.STEP2_DESC' },
    { num: '03', icon: 'fa-file-signature',      title: 'SERVICES.STEP3_TITLE', desc: 'SERVICES.STEP3_DESC' },
  ];

  readonly stats = [
    { value: '10,000+', label: 'SERVICES.STAT1_LABEL' },
    { value: '200+',    label: 'SERVICES.STAT2_LABEL' },
    { value: '98%',     label: 'SERVICES.STAT3_LABEL' },
    { value: '16+',     label: 'SERVICES.STAT4_LABEL' },
  ];
}
