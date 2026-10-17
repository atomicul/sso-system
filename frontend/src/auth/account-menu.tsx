import { Link } from '@tanstack/react-router'
import { CircleUserRoundIcon, LogOutIcon, UserPenIcon } from 'lucide-react'
import { useAuth } from '@/auth/use-auth'
import { Button, buttonVariants } from '@/components/ui/button'
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
  DrawerTrigger,
} from '@/components/ui/drawer'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { useMediaQuery } from '@/lib/use-media-query'
import { cn } from '@/lib/utils'

type AccountMenuProps = { email: string }

const triggerButton = (
  <Button variant="ghost" size="icon-lg" aria-label="Account" />
)
const accountIcon = <CircleUserRoundIcon className="size-6" />

/** Shows who is logged in, with links to the profile and to log out: a menu by the button, or a drawer on phones. */
export function AccountMenu({ email }: AccountMenuProps) {
  const isWide = useMediaQuery('(min-width: 40rem)')

  return isWide ? (
    <AccountDropdown email={email} />
  ) : (
    <AccountDrawer email={email} />
  )
}

function AccountDropdown({ email }: AccountMenuProps) {
  const { logout } = useAuth()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={triggerButton}>
        {accountIcon}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-auto max-w-72 min-w-56">
        <DropdownMenuGroup>
          <DropdownMenuLabel>
            Logged in as
            <span className="text-foreground block truncate text-sm">
              {email}
            </span>
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem render={<Link to="/profile" />}>
          <UserPenIcon />
          Profile
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => logout()}>
          <LogOutIcon />
          Log out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function AccountDrawer({ email }: AccountMenuProps) {
  const { logout } = useAuth()

  return (
    <Drawer showSwipeHandle>
      <DrawerTrigger render={triggerButton}>{accountIcon}</DrawerTrigger>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Logged in as</DrawerTitle>
          <DrawerDescription className="truncate">{email}</DrawerDescription>
        </DrawerHeader>
        <DrawerFooter className="pt-4">
          <DrawerClose
            nativeButton={false}
            render={
              <Link
                to="/profile"
                className={cn(
                  buttonVariants({ variant: 'outline', size: 'lg' }),
                )}
              />
            }
          >
            <UserPenIcon />
            Profile
          </DrawerClose>
          <Button variant="outline" size="lg" onClick={() => logout()}>
            <LogOutIcon />
            Log out
          </Button>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  )
}
