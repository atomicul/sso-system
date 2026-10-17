import { zodResolver } from '@hookform/resolvers/zod'
import { PencilIcon } from 'lucide-react'
import { useState } from 'react'
import { type FieldPath, type UseFormReturn, useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { getGetApiProfileQueryKey, usePutApiProfile } from '@/api/generated'
import { PutApiProfileBody } from '@/api/generated/index.zod'
import type { Profile } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSeparator,
  FieldSet,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { setServerErrors } from '@/lib/server-errors'
import { cn } from '@/lib/utils'

type ProfileInputProps = {
  name: Exclude<FieldPath<Profile>, 'address'>
  label: string
  autoComplete: string
  type?: 'text' | 'date' | 'tel'
  fullWidth?: boolean
}

const personalInputs: ProfileInputProps[] = [
  { name: 'givenName', label: 'First name', autoComplete: 'given-name' },
  { name: 'familyName', label: 'Last name', autoComplete: 'family-name' },
  { name: 'birthdate', label: 'Birthdate', autoComplete: 'bday', type: 'date' },
  {
    name: 'phoneNumber',
    label: 'Phone number',
    autoComplete: 'tel',
    type: 'tel',
  },
]

const addressInputs: ProfileInputProps[] = [
  {
    name: 'address.streetAddress',
    label: 'Street address',
    autoComplete: 'street-address',
    fullWidth: true,
  },
  { name: 'address.locality', label: 'City', autoComplete: 'address-level2' },
  {
    name: 'address.region',
    label: 'State or region',
    autoComplete: 'address-level1',
  },
  {
    name: 'address.postalCode',
    label: 'Postal code',
    autoComplete: 'postal-code',
  },
  { name: 'address.country', label: 'Country', autoComplete: 'country-name' },
]

/** An empty input stands for null, as the API saves blank values. */
const emptyAsNull = {
  setValueAs: (value: string) => (value === '' ? null : value),
}

/**
 * Shows the profile apps receive when the user opens them, editable after pressing Edit;
 * failures show under the fields.
 */
export function ProfileForm({ profile }: { profile: Profile }) {
  const [isEditing, setIsEditing] = useState(false)
  const form = useForm<Profile>({
    resolver: zodResolver(PutApiProfileBody),
    defaultValues: profile,
  })
  const { mutateAsync: putProfile } = usePutApiProfile({
    mutation: {
      meta: { handlesErrors: true, invalidates: [getGetApiProfileQueryKey()] },
    },
  })
  const { errors, isSubmitting } = form.formState

  async function submit(values: Profile) {
    try {
      form.reset(await putProfile({ data: values }))
      setIsEditing(false)
      toast.success('Your profile is saved.')
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  function edit() {
    setIsEditing(true)
    form.setFocus('givenName')
  }

  function cancel() {
    form.reset()
    setIsEditing(false)
  }

  return (
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <Card>
        <CardHeader>
          <CardTitle>Personal information</CardTitle>
          <CardDescription>
            Apps receive these details the next time you open them from here.
          </CardDescription>
          {!isEditing && (
            <CardAction>
              <Button type="button" variant="outline" onClick={edit}>
                <PencilIcon />
                Edit
              </Button>
            </CardAction>
          )}
        </CardHeader>
        <CardContent>
          <FieldGroup>
            <ProfileFieldSet
              legend="About you"
              inputs={personalInputs}
              form={form}
              readOnly={!isEditing}
            />
            <FieldSeparator />
            <ProfileFieldSet
              legend="Address"
              inputs={addressInputs}
              form={form}
              readOnly={!isEditing}
            />
            <FieldError errors={[errors.root]} />
          </FieldGroup>
        </CardContent>
        {isEditing && (
          <CardFooter className="justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={cancel}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              Save
            </Button>
          </CardFooter>
        )}
      </Card>
    </form>
  )
}

type ProfileFormState = {
  form: UseFormReturn<Profile>
  readOnly: boolean
}

function ProfileFieldSet({
  legend,
  inputs,
  ...state
}: ProfileFormState & { legend: string; inputs: ProfileInputProps[] }) {
  return (
    <FieldSet>
      <FieldLegend>{legend}</FieldLegend>
      <div className="grid gap-4 sm:grid-cols-2">
        {inputs.map((input) => (
          <ProfileInput key={input.name} {...state} {...input} />
        ))}
      </div>
    </FieldSet>
  )
}

function ProfileInput({
  form,
  readOnly,
  name,
  label,
  autoComplete,
  type = 'text',
  fullWidth = false,
}: ProfileInputProps & ProfileFormState) {
  const { error } = form.getFieldState(name, form.formState)

  return (
    <Field data-invalid={!!error} className={cn(fullWidth && 'sm:col-span-2')}>
      <FieldLabel htmlFor={name}>{label}</FieldLabel>
      <Input
        id={name}
        type={type}
        autoComplete={autoComplete}
        aria-invalid={!!error}
        readOnly={readOnly}
        className="read-only:bg-muted/50 read-only:border-transparent"
        {...form.register(name, emptyAsNull)}
      />
      <FieldError errors={[error]} />
    </Field>
  )
}
