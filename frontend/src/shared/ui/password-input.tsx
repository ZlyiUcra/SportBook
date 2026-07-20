import * as React from "react"
import { Eye, EyeOff } from "lucide-react"

import { cn } from "@/shared/lib/utils"
import { Input } from "./input"

type PasswordInputProps = Omit<React.ComponentProps<typeof Input>, "type"> & {
  /** Screen-reader label for the toggle when the password is currently hidden. */
  showPasswordLabel?: string
  /** Screen-reader label for the toggle when the password is currently visible. */
  hidePasswordLabel?: string
}

/** A password field with a right-aligned eye toggle to reveal/hide the typed value. */
export const PasswordInput = React.forwardRef<HTMLInputElement, PasswordInputProps>(
  ({ className, showPasswordLabel = "Show password", hidePasswordLabel = "Hide password", ...props }, ref) => {
    const [visible, setVisible] = React.useState(false)

    return (
      <div className="relative">
        <Input ref={ref} type={visible ? "text" : "password"} className={cn("pr-8", className)} {...props} />
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
          aria-label={visible ? hidePasswordLabel : showPasswordLabel}
        >
          {visible ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
        </button>
      </div>
    )
  },
)
PasswordInput.displayName = "PasswordInput"
