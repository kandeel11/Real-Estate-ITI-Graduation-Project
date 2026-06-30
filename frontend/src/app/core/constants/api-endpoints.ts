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
    REGISTER_COMPANY: '/auth/register-company',
    VERIFY_EMAIL: '/auth/verify-email',
    RESEND_VERIFICATION: '/auth/resend-verification',
    SEND_OTP: '/auth/send-otp',
    VERIFY_OTP: '/auth/verify-otp',
    REFRESH_TOKEN: '/auth/refresh',
    LOGOUT: '/auth/logout',
    FORGOT_PASSWORD: '/auth/forgot-password',
    RESET_PASSWORD: '/auth/reset-password',
    CHANGE_PASSWORD: '/auth/change-password',
  },

  // ==================== PROPERTIES MODULE ====================
  PROPERTIES: {
    GET_ALL: '/properties', // admin-only on the backend
    GET_ACTIVE: '/properties/active',
    GET_MINE: '/properties/mine',
    GET_PAGINATED: '/properties/paginated',
    GET_BY_ID: (id: string) => `/properties/${id}`,
    CREATE: '/properties',
    UPDATE: (id: string) => `/properties/${id}`,
    DELETE: (id: string) => `/properties/${id}`,
    SUBMIT: (id: string) => `/properties/${id}/submit`,
    ASSIGN_BROKER: (id: string) => `/properties/${id}/assign-broker`,
    REQUEST_AI_DESCRIPTION: (id: string) => `/properties/${id}/request-ai-description`,
    MEDIA: (id: string) => `/properties/${id}/media`,
    SET_PRIMARY_MEDIA: (id: string, mediaId: string) =>
      `/properties/${id}/media/${mediaId}/primary`,
    DELETE_MEDIA: (id: string, mediaId: string) => `/properties/${id}/media/${mediaId}`,
    AMENITIES: (id: string) => `/properties/${id}/amenities`,
    REMOVE_AMENITY: (id: string, amenityId: string) => `/properties/${id}/amenities/${amenityId}`,
    GET_BY_CITY: (city: string) => `/properties/city/${city}`,
    GET_FEATURED: '/properties/featured',
    SEARCH: '/properties/search',
    SET_FEATURED: (id: string) => `/properties/${id}/feature`,
    MARK_RENTED: (id: string) => `/properties/${id}/rented`,
    // Saved properties (tenant wishlist)
    SAVED: '/properties/saved',
    SAVED_IDS: '/properties/saved/ids',
    SAVE: (id: string) => `/properties/${id}/save`,
  },

  // ==================== AMENITIES CATALOG ====================
  AMENITIES: {
    GET_ALL: '/amenities',
    GET_BY_PROPERTY: (propertyId: string) => `/amenities/property/${propertyId}`,
  },

  // ==================== INQUIRIES MODULE ====================
  INQUIRIES: {
    CREATE: '/inquiries',
    CAN_CREATE: '/inquiries/can-create',
    MINE: '/inquiries/mine',
    BY_PROPERTY: (propertyId: string) => `/inquiries/property/${propertyId}`,
  },

  // ==================== AI MODULE ====================
  AI: {
    DESCRIPTION_BY_PROPERTY: (propertyId: string) => `/AI/descriptions/by-property/${propertyId}`,
    ACCEPT_DESCRIPTION: (generationId: string) => `/AI/descriptions/${generationId}/accept`,
    CHATBOT_CHAT: '/AI/chatbot/chat',
    CHATBOT_SESSIONS: '/AI/chatbot/sessions',
    CHATBOT_HISTORY: (sessionId: string) => `/AI/chatbot/sessions/${sessionId}/history`,
    ADMIN_ANALYTICS_CHAT: '/AI/admin/analytics/chat',
  },

  // ==================== COMPANIES MODULE ====================
  // Matches backend CompaniesController ("mine" routes read CompanyId from JWT)
  COMPANIES: {
    GET_BY_ID: (id: string) => `/companies/${id}`,
    MINE: '/companies/mine',
    UPDATE_MINE: '/companies/mine',
    MINE_PROPERTIES: '/companies/mine/properties',
    MINE_BROKERS: '/companies/mine/brokers',
    MINE_STATS: '/companies/mine/stats',
    MINE_ACTIVITY: '/companies/mine/activity',
    MINE_INQUIRIES: '/companies/mine/inquiries',
    MINE_LOGO: '/companies/mine/logo',
    MINE_RATINGS_SUMMARY: '/companies/mine/ratings-summary',
    UPDATE: (id: string) => `/companies/${id}`,
    VERIFY: (id: string) => `/companies/${id}/verify`,
    GET_PROPERTIES: (id: string) => `/companies/${id}/properties`,
    GET_BROKERS: (id: string) => `/companies/${id}/brokers`,
  },

  // ==================== BROKERS MODULE ====================
  // Matches backend BrokersController (nested under /companies)
  BROKERS: {
    ADD_TO_MINE: '/companies/mine/brokers',
    ADD: (companyId: string) => `/companies/${companyId}/brokers`,
    DEACTIVATE_MINE: (brokerId: string) => `/companies/mine/brokers/${brokerId}`,
    REACTIVATE_MINE: (brokerId: string) => `/companies/mine/brokers/${brokerId}/reactivate`,
    DELETE_MINE: (brokerId: string) => `/companies/mine/brokers/${brokerId}/permanent`,
  },

  // ==================== MAINTENANCE MODULE ====================
  MAINTENANCE: {
    TICKETS: {
      GET_ALL: '/maintenance/tickets',
      GET_BY_ID: (id: string) => `/maintenance/tickets/${id}`,
      CREATE: '/maintenance/tickets',
      UPDATE: (id: string) => `/maintenance/tickets/${id}`,
      DELETE: (id: string) => `/maintenance/tickets/${id}`,
      GET_BY_PROPERTY: (propertyId: string) => `/maintenance/tickets/property/${propertyId}`,
      GET_BY_STATUS: (status: string) => `/maintenance/tickets/status/${status}`,
    },
    STATUS_UPDATES: {
      GET_HISTORY: (ticketId: string) => `/maintenance/tickets/${ticketId}/status-history`,
      UPDATE_STATUS: (ticketId: string) => `/maintenance/tickets/${ticketId}/status`,
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
    GET_BY_PROPERTY: (propertyId: string) => `/leases/property/${propertyId}`,
    RENEW: (id: string) => `/leases/${id}/renew`,
    TERMINATE: (id: string) => `/leases/${id}/terminate`,
    GET_ACTIVE: '/leases/active',
    GET_EXPIRING_SOON: '/leases/expiring-soon',
  },

  // ==================== SUBSCRIPTIONS MODULE ====================
  // MINE/MINE_USAGE match the backend SubscriptionsController;
  // plan catalog lives at /subscription-plans
  SUBSCRIPTIONS: {
    GET_PLANS: '/subscription-plans',
    MINE: '/subscriptions/mine',
    MINE_USAGE: '/subscriptions/mine/usage',
    MINE_PAYMENTS: '/subscriptions/mine/payments',
    MINE_UPGRADE: '/subscriptions/mine/upgrade',
    MINE_CANCEL: '/subscriptions/mine/cancel',
    SELECT_PLAN: '/subscriptions/select-plan',
  },

  // ==================== PAYMENTS MODULE ====================
  PAYMENTS: {
    GET_ALL: '/payments',
    GET_MY: '/payments/my',
    GET_BY_ID: (id: string) => `/payments/${id}`,
    PROCESS: (id: string) => `/payments/${id}/process`,
    GET_OVERDUE: '/payments/overdue',
    GET_SUMMARY: '/payments/summary',
    GET_BY_STATUS: (status: string) => `/payments/status/${status}`,
    APPLY_LATE_FEE: (id: string) => `/payments/${id}/late-fee`,
    MARK_EXTERNAL: (id: string) => `/payments/${id}/mark-external`,
    GENERATE_LINK: (id: string) => `/payments/${id}/generate-link`,
    OWNER_FINANCIAL_SUMMARY: '/payments/owner/financial-summary',
    WAIVE_LATE_FEE: (id: string) => `/payments/${id}/waive-late-fee`,
    GET_RECEIPT: (id: string) => `/payments/${id}/receipt`,
    REFUND: (id: string) => `/payments/${id}/refund`,
    GENERATE_MONTHLY: '/payments/generate-monthly',
    GENERATE_FOR_LEASE: (leaseId: string) => `/payments/generate-for-lease/${leaseId}`,
    SUBSCRIPTIONS_CHECKOUT: '/payments/subscriptions/checkout',
    STRIPE_WEBHOOK: '/payments/webhook/stripe',
  },

  // ==================== TENANT PAYMENTS ====================
  TENANT_PAYMENTS: {
    GET_ALL: '/tenant/payments',
    GET_SCHEDULE: '/tenant/payments/schedule',
  },

  // ==================== OWNER PAYMENTS ====================
  OWNER_PAYMENTS: {
    GET_ALL: '/owners/me/payments',
  },

  // ==================== BROKER PAYMENTS ====================
  BROKER_PAYMENTS: {
    GET_ALL: '/brokers/me/payments',
    GET_SCHEDULE: '/brokers/me/payments/schedule',
  },

  // ==================== COMPANY PAYMENTS ====================
  COMPANY_PAYMENTS: {
    GET_ALL: '/companies/mine/payments',
    GET_SCHEDULE: '/companies/mine/payments/schedule',
  },

  // ==================== WALLET MODULE ====================
  WALLET: {
    BALANCE: '/wallet/balance',
    LEDGER: '/wallet/ledger',
    WITHDRAW: '/wallet/withdrawals',
    MY_WITHDRAWALS: '/wallet/withdrawals',
    ALL_WITHDRAWALS: '/wallet/admin/withdrawals',
    APPROVE_WITHDRAWAL: (id: string) => `/wallet/admin/withdrawals/${id}/approve`,
    REJECT_WITHDRAWAL: (id: string) => `/wallet/admin/withdrawals/${id}/reject`,
  },

  // ==================== LEASE PAYMENT SCHEDULE ====================
  LEASE_SCHEDULE: {
    GET: (leaseId: string) => `/leases/${leaseId}/payment-schedule`,
  },

  // ==================== SMART SEARCH MODULE ====================
  SMART_SEARCH: {
    SEARCH: '/smart-search/search',
    ADVANCED_SEARCH: '/smart-search/advanced',
    GET_FILTERS: '/smart-search/filters',
    GET_SUGGESTIONS: '/smart-search/suggestions',
    SAVE_SEARCH: '/smart-search/save-search',
    GET_SAVED_SEARCHES: '/smart-search/saved-searches',
    DELETE_SAVED_SEARCH: (searchId: string) => `/smart-search/saved-searches/${searchId}`,
  },

  // ==================== AI CHAT & RAG MODULE ====================
  CHAT_RAG: {
    SEND_MESSAGE: '/chat-rag/message',
    GET_CONVERSATION: (conversationId: string) => `/chat-rag/conversation/${conversationId}`,
    GET_CONVERSATIONS: '/chat-rag/conversations',
    CREATE_CONVERSATION: '/chat-rag/conversation',
    DELETE_CONVERSATION: (conversationId: string) => `/chat-rag/conversation/${conversationId}`,
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

  // ==================== OWNERSHIP VERIFICATION MODULE ====================
  OWNERSHIP_VERIFICATIONS: {
    PENDING_REVIEWS: '/ownershipverifications/admin/pending',
    GET_BY_ID: (id: string) => `/ownershipverifications/${id}`,
    APPROVE: (id: string) => `/ownershipverifications/${id}/approve`,
    REJECT: (id: string) => `/ownershipverifications/${id}/reject`,
  },

  // ==================== ANALYTICS & REPORTING ====================
  ANALYTICS: {
    GET_DASHBOARD_METRICS: '/analytics/dashboard',
    GET_PROPERTY_ANALYTICS: (propertyId: string) => `/analytics/property/${propertyId}`,
    GET_USER_ANALYTICS: '/analytics/user',
    EXPORT_REPORT: '/analytics/export',
  },
} as const;
