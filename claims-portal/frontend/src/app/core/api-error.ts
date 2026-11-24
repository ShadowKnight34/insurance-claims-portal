import { HttpErrorResponse } from '@angular/common/http';

export function readError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { detail?: string; title?: string; errors?: Record<string, string[]> } | string | null;
    if (typeof body === 'string' && body.trim()) {
      return body;
    }

    if (body && typeof body === 'object') {
      if (body.errors) {
        const messages = Object.values(body.errors).flat().filter(Boolean);
        if (messages.length > 0) {
          return messages.join(' ');
        }
      }

      if (body.detail) {
        return body.detail;
      }

      if (body.title && error.status >= 400) {
        return body.title;
      }
    }

    if (error.status === 0) {
      return 'The claims API is not reachable. Start the backend on http://localhost:5080.';
    }

    return error.message || 'The request failed.';
  }

  return 'Something went wrong. Please try again.';
}
