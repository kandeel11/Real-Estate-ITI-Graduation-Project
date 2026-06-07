/**
 * Centralized API Endpoints Configuration
 * Maps all backend ASP.NET Core Modular Monolith endpoints
 * Assumes base URL from environment.apiUrl
 */

export const API_ENDPOINTS = {
  // ==================== AUTH MODULE ====================
  AUTH: {
    LOGIN: '/auth/login',
    REGISTER: '/auth/register',
    VERIFY_EMAIL: '/auth/verify-email',
    REQUEST_OTP: '/auth/request-otp',
    VERIFY_OTP: '/auth/verify-otp',
    REFRESH_TOKEN: '/auth/refresh-token',
    LOGOUT: '/auth/logout',
    FORGOT_PASSWORD: '/auth/forgot-password',
    RESET_PASSWORD: '/auth/reset-password',
  },

  // ==================== PROPERTIES MODULE ====================
  PROPERTIES: {
    GET_ALL: '/properties',
    GET_PAGINATED: '/properties/paginated',
    GET_BY_ID: (id: string) => `/properties/${id}`,
    CREATE: '/properties',
    UPDATE: (id: string) => `/properties/${id}`,
    DELETE: (id: string) => `/properties/${id}`,
    UPLOAD_IMAGES: (id: string) => `/properties/${id}/images`,
    DELETE_IMAGE: (id: string, imageId: string) =>
      `/properties/${id}/images/${imageId}`,
    GET_BY_CITY: (city: string) => `/properties/city/${city}`,
    GET_FEATURED: '/properties/featured',
    SEARCH: '/properties/search',
  },

  // ==================== COMPANIES MODULE ====================
  COMPANIES: {
    GET_ALL: '/companies',
    GET_BY_ID: (id: string) => `/companies/${id}`,
    CREATE: '/companies',
    UPDATE: (id: string) => `/companies/${id}`,
    DELETE: (id: string) => `/companies/${id}`,
    GET_PROPERTIES: (id: string) => `/companies/${id}/properties`,
    GET_BROKERS: (id: string) => `/companies/${id}/brokers`,
    UPDATE_STATUS: (id: string) => `/companies/${id}/status`,
  },

  // ==================== BROKERS MODULE ====================
  BROKERS: {
    GET_ALL: '/brokers',
    GET_BY_ID: (id: string) => `/brokers/${id}`,
    CREATE: '/brokers',
    UPDATE: (id: string) => `/brokers/${id}`,
    DELETE: (id: string) => `/brokers/${id}`,
    GET_LISTINGS: (id: string) => `/brokers/${id}/listings`,
    GET_PERFORMANCE: (id: string) => `/brokers/${id}/performance`,
    GET_COMMISSION_HISTORY: (id: string) =>
      `/brokers/${id}/commission-history`,
  },

  // ==================== MAINTENANCE MODULE ====================
  MAINTENANCE: {
    TICKETS: {
      GET_ALL: '/maintenance/tickets',
      GET_BY_ID: (id: string) => `/maintenance/tickets/${id}`,
      CREATE: '/maintenance/tickets',
      UPDATE: (id: string) => `/maintenance/tickets/${id}`,
      DELETE: (id: string) => `/maintenance/tickets/${id}`,
      GET_BY_PROPERTY: (propertyId: string) =>
        `/maintenance/tickets/property/${propertyId}`,
      GET_BY_STATUS: (status: string) =>
        `/maintenance/tickets/status/${status}`,
    },
    STATUS_UPDATES: {
      GET_HISTORY: (ticketId: string) =>
        `/maintenance/tickets/${ticketId}/status-history`,
      UPDATE_STATUS: (ticketId: string) =>
        `/maintenance/tickets/${ticketId}/status`,
      ADD_NOTE: (ticketId: string) => `/maintenance/tickets/${ticketId}/notes`,
    },
    REPORTS: {
      GET_BY_MONTH: '/maintenance/reports/monthly',
      GET_STATISTICS: '/maintenance/reports/statistics',
      GET_COMPLETION_RATE: '/maintenance/reports/completion-rate',
    },
  },

  // ==================== LEASES MODULE ====================
  LEASES: {
    GET_ALL: '/leases',
    GET_BY_ID: (id: string) => `/leases/${id}`,
    CREATE: '/leases',
    UPDATE: (id: string) => `/leases/${id}`,
    DELETE: (id: string) => `/leases/${id}`,
    GET_BY_TENANT: (tenantId: string) => `/leases/tenant/${tenantId}`,
    GET_BY_PROPERTY: (propertyId: string) =>
      `/leases/property/${propertyId}`,
    RENEW: (id: string) => `/leases/${id}/renew`,
    TERMINATE: (id: string) => `/leases/${id}/terminate`,
    GET_ACTIVE: '/leases/active',
    GET_EXPIRING_SOON: '/leases/expiring-soon',
  },

  // ==================== SUBSCRIPTIONS MODULE ====================
  SUBSCRIPTIONS: {
    GET_PLANS: '/subscriptions/plans',
    GET_USER_SUBSCRIPTION: '/subscriptions/user',
    SUBSCRIBE: '/subscriptions/subscribe',
    UPGRADE_PLAN: '/subscriptions/upgrade',
    CANCEL_SUBSCRIPTION: '/subscriptions/cancel',
    GET_BILLING_HISTORY: '/subscriptions/billing-history',
    GET_USAGE: '/subscriptions/usage',
  },

  // ==================== PAYMENTS MODULE ====================
  PAYMENTS: {
    CREATE_INTENT: '/payments/create-intent',
    CONFIRM_PAYMENT: '/payments/confirm',
    GET_PAYMENT_HISTORY: '/payments/history',
    GET_INVOICE: (invoiceId: string) => `/payments/invoice/${invoiceId}`,
    DOWNLOAD_INVOICE: (invoiceId: string) =>
      `/payments/invoice/${invoiceId}/download`,
    PROCESS_REFUND: (paymentId: string) => `/payments/${paymentId}/refund`,
  },

  // ==================== SMART SEARCH MODULE ====================
  SMART_SEARCH: {
    SEARCH: '/smart-search/search',
    ADVANCED_SEARCH: '/smart-search/advanced',
    GET_FILTERS: '/smart-search/filters',
    GET_SUGGESTIONS: '/smart-search/suggestions',
    SAVE_SEARCH: '/smart-search/save-search',
    GET_SAVED_SEARCHES: '/smart-search/saved-searches',
    DELETE_SAVED_SEARCH: (searchId: string) =>
      `/smart-search/saved-searches/${searchId}`,
  },

  // ==================== AI CHAT & RAG MODULE ====================
  CHAT_RAG: {
    SEND_MESSAGE: '/chat-rag/message',
    GET_CONVERSATION: (conversationId: string) =>
      `/chat-rag/conversation/${conversationId}`,
    GET_CONVERSATIONS: '/chat-rag/conversations',
    CREATE_CONVERSATION: '/chat-rag/conversation',
    DELETE_CONVERSATION: (conversationId: string) =>
      `/chat-rag/conversation/${conversationId}`,
    SEARCH_DOCUMENTS: '/chat-rag/search-documents',
    UPLOAD_DOCUMENT: '/chat-rag/upload-document',
  },

  // ==================== USER PROFILE MODULE ====================
  USER_PROFILE: {
    GET_PROFILE: '/user/profile',
    UPDATE_PROFILE: '/user/profile',
    UPLOAD_AVATAR: '/user/avatar',
    GET_PREFERENCES: '/user/preferences',
    UPDATE_PREFERENCES: '/user/preferences',
    GET_NOTIFICATIONS: '/user/notifications',
    MARK_NOTIFICATION_READ: (notificationId: string) =>
      `/user/notifications/${notificationId}/read`,
  },

  // ==================== ANALYTICS & REPORTING ====================
  ANALYTICS: {
    GET_DASHBOARD_METRICS: '/analytics/dashboard',
    GET_PROPERTY_ANALYTICS: (propertyId: string) =>
      `/analytics/property/${propertyId}`,
    GET_USER_ANALYTICS: '/analytics/user',
    EXPORT_REPORT: '/analytics/export',
  },
} as const;
