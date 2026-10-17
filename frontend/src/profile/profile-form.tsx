import { zodResolver } from '@hookform/resolvers/zod'
import { type FieldPath, type UseFormReturn, useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { getGetApiProfileQueryKey, usePutApiProfile } from '@/api/generated'
import { PutApiProfileBody } from '@/api/generated/index.zod'
import type { Profile } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSet,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { setServerErrors } from '@/lib/server-errors'

type ProfileInputProps = {
  name: Exclude<FieldPath<Profile>, 'address'>
  label: string
  autoComplete: string
  type?: 'text' | 'date' | 'tel'
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

/** Edits the profile apps receive when the user opens them; failures show under the fields. */
export function ProfileForm({ profile }: { profile: Profile }) {
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
      toast.success('Your profile is saved.')
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <FieldGroup>
        {personalInputs.map((input) => (
          <ProfileInput key={input.name} form={form} {...input} />
        ))}
        <FieldSet>
          <FieldLegend>Address</FieldLegend>
          {addressInputs.map((input) => (
            <ProfileInput key={input.name} form={form} {...input} />
          ))}
        </FieldSet>
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          Save
        </Button>
      </FieldGroup>
    </form>
  )
}

function ProfileInput({
  form,
  name,
  label,
  autoComplete,
  type = 'text',
}: ProfileInputProps & { form: UseFormReturn<Profile> }) {
  const { error } = form.getFieldState(name, form.formState)

  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={name}>{label}</FieldLabel>
      <Input
        id={name}
        type={type}
        autoComplete={autoComplete}
        aria-invalid={!!error}
        {...form.register(name, emptyAsNull)}
      />
      <FieldError errors={[error]} />
    </Field>
  )
}
