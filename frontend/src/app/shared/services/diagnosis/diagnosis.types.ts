export type Diagnosis = {
  diagnosisID: number;
  diagnosis: string;
  notes: string | null;
};

/**
 * The create and update bodies are the same shape, so one type covers both.
 * The visit is in the URL, never in the body.
 */
export type DiagnosisRequest = {
  diagnosis: string;
  notes?: string;
};
