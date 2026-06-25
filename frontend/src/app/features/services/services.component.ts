import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { InViewDirective } from '../../shared/directives/in-view.directive';

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [RouterLink, InViewDirective],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss',
})
export class ServicesComponent {
  readonly services = [
    {
      icon: 'fa-house-chimney',
      color: '#4f46e5',
      bg: 'rgba(79,70,229,.1)',
      title: 'Property Listing & Management',
      desc: 'Publish, manage and showcase your properties with rich media, AI-generated descriptions, and real-time availability tracking.',
    },
    {
      icon: 'fa-wand-magic-sparkles',
      color: '#8b5dee',
      bg: 'rgba(139,93,238,.1)',
      title: 'AI-Powered Smart Search',
      desc: 'Our semantic AI engine understands natural language queries — find the perfect property by simply describing what you need.',
    },
    {
      icon: 'fa-users',
      color: '#0891b2',
      bg: 'rgba(8,145,178,.1)',
      title: 'Tenant & Lease Management',
      desc: 'Onboard tenants digitally, generate lease agreements, track renewals, and manage the full tenant lifecycle in one place.',
    },
    {
      icon: 'fa-chart-line',
      color: '#059669',
      bg: 'rgba(5,150,105,.1)',
      title: 'Analytics & Reporting',
      desc: 'Real-time dashboards and exportable reports give you deep insight into occupancy, revenue, and property performance.',
    },
    {
      icon: 'fa-screwdriver-wrench',
      color: '#d97706',
      bg: 'rgba(217,119,6,.1)',
      title: 'Maintenance Services',
      desc: 'Connect with vetted maintenance companies, raise tickets, track progress, and keep your properties in top condition.',
    },
    {
      icon: 'fa-crown',
      color: '#dc2626',
      bg: 'rgba(220,38,38,.1)',
      title: 'Flexible Subscription Plans',
      desc: 'From independent landlords to enterprise agencies — choose the plan that scales with your portfolio and business needs.',
    },
  ];

  readonly roles = [
    {
      icon: 'fa-house-chimney',
      gradient: 'linear-gradient(135deg,#4f46e5,#6366f1)',
      label: 'Property Owners',
      points: ['List unlimited properties', 'Track rental income', 'Manage maintenance', 'Lease automation'],
    },
    {
      icon: 'fa-key',
      gradient: 'linear-gradient(135deg,#0891b2,#38bdf8)',
      label: 'Tenants',
      points: ['Smart property search', 'Digital lease signing', 'Rent payment history', 'Maintenance requests'],
    },
    {
      icon: 'fa-handshake',
      gradient: 'linear-gradient(135deg,#f59e0b,#fbbf24)',
      label: 'Brokers',
      points: ['Client lead management', 'Commission tracking', 'Performance analytics', 'Shared property pool'],
    },
    {
      icon: 'fa-building',
      gradient: 'linear-gradient(135deg,#059669,#34d399)',
      label: 'Real Estate Companies',
      points: ['Multi-broker management', 'Portfolio overview', 'AI property descriptions', 'Company-level analytics'],
    },
  ];

  readonly steps = [
    { num: '01', icon: 'fa-user-plus', title: 'Create Your Account', desc: 'Sign up in under 2 minutes — choose your role and verify your email to unlock your personalised dashboard.' },
    { num: '02', icon: 'fa-house-circle-check', title: 'List or Find Properties', desc: 'Owners list their properties with rich media; tenants and brokers use AI-powered search to find the perfect match.' },
    { num: '03', icon: 'fa-file-signature', title: 'Manage Everything in One Place', desc: 'From lease generation to rent payments, maintenance and analytics — the Estatein platform handles the full lifecycle.' },
  ];

  readonly stats = [
    { value: '10,000+', label: 'Properties Listed' },
    { value: '200+', label: 'Happy Customers' },
    { value: '98%', label: 'Satisfaction Rate' },
    { value: '16+', label: 'Years Experience' },
  ];
}
