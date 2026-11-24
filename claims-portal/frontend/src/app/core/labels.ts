const STATUS_LABELS: Record<string, string> = {
  Submitted: 'Submitted',
  UnderReview: 'Under review',
  AdditionalInformationRequired: 'Awaiting information',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Settled: 'Settled'
};

export function statusLabel(status: string | null | undefined): string {
  if (!status) {
    return 'New';
  }

  return STATUS_LABELS[status] ?? status;
}

export function statusClass(status: string | null | undefined): string {
  switch (status) {
    case 'Submitted':
      return 'status status-submitted';
    case 'UnderReview':
      return 'status status-review';
    case 'AdditionalInformationRequired':
      return 'status status-waiting';
    case 'Approved':
      return 'status status-approved';
    case 'Rejected':
      return 'status status-rejected';
    case 'Settled':
      return 'status status-settled';
    default:
      return 'status';
  }
}

const moneyFormat = new Intl.NumberFormat('en-SG', {
  style: 'currency',
  currency: 'SGD',
  minimumFractionDigits: 2
});

export function money(amount: number | null | undefined): string {
  return moneyFormat.format(amount ?? 0);
}

export function shortDate(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  const date = new Date(value.includes('T') ? value : `${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat('en-SG', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  }).format(date);
}

export function dateTime(value: string | null | undefined): string {
  if (!value) {
    return '—';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat('en-SG', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
}

export function fileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
