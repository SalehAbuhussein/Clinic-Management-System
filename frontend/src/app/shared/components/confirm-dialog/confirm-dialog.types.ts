export type ConfirmDialogData = {
  /** The question, as a question. */
  title: string;

  /** What happens if they go ahead, and what survives if they do. */
  message: string;

  /** The confirm button's words. Never "Yes": a label should name the action. */
  confirmLabel: string;

  /** The dismiss button's words. Defaults to "Keep". */
  dismissLabel?: string;

  /** Paints the confirm button in the error role. For anything with no way back. */
  destructive?: boolean;
};
