import { ParsedQuery } from './parsed-query';
import { PropertyResult } from './property-result';

export interface SmartSearchRequest {
  query: string;
  page?: number;
  pageSize?: number;
}

export interface SmartSearchResponse {
  queryId: string;
  parsedQuery: ParsedQuery;
  totalResults: number;
  properties: PropertyResult[];
  page: number;
  pageSize: number;
  tokensUsed: number;
}
