import { z } from 'zod'

/** Minimum comment length required when editing an existing review (007 data-model.md) - a first-time submission has no such requirement. */
export const EDIT_COMMENT_MIN_LENGTH = 10

/**
 * A first-time submission keeps the comment optional; editing an existing review (isEdit) requires
 * a comment of at least {@link EDIT_COMMENT_MIN_LENGTH} characters (007 US2) - modeled as a refine
 * so both modes share the same output shape.
 */
export function makeReviewFormSchema(isEdit: boolean) {
  return z
    .object({
      rating: z.number().min(1).max(5),
      comment: z.string().optional(),
    })
    .refine((values) => !isEdit || (values.comment?.trim().length ?? 0) >= EDIT_COMMENT_MIN_LENGTH, {
      message: 'commentTooShort',
      path: ['comment'],
    })
}

export type ReviewFormValues = z.infer<ReturnType<typeof makeReviewFormSchema>>
