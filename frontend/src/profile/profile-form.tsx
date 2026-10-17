import { zodResolver } from '@hookform/resolvers/zod'
import { type FieldPath, type UseFormReturn, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  getGetApiProfileQueryKey,
  useGetApiProfile,
  usePutApiProfile,
} from '@/api/generated'
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
  label:
    | 'givenName'
    | 'familyName'
    | 'birthdate'
    | 'phoneNumber'
    | 'streetAddress'
    | 'locality'
    | 'region'
    | 'postalCode'
    | 'country'
  autoComplete: string
  type?: 'text' | 'date' | 'tel'
}

const personalInputs: ProfileInputProps[] = [
  { name: 'givenName', label: 'givenName', autoComplete: 'given-name' },
  { name: 'familyName', label: 'familyName', autoComplete: 'family-name' },
  {
    name: 'birthdate',
    label: 'birthdate',
    autoComplete: 'bday',
    type: 'date',
  },
  {
    name: 'phoneNumber',
    label: 'phoneNumber',
    autoComplete: 'tel',
    type: 'tel',
  },
]

const addressInputs: ProfileInputProps[] = [
  {
    name: 'address.streetAddress',
    label: 'streetAddress',
    autoComplete: 'street-address',
  },
  {
    name: 'address.locality',
    label: 'locality',
    autoComplete: 'address-level2',
  },
  { name: 'address.region', label: 'region', autoComplete: 'address-level1' },
  {
    name: 'address.postalCode',
    label: 'postalCode',
    autoComplete: 'postal-code',
  },
  { name: 'address.country', label: 'country', autoComplete: 'country-name' },
]

/** An empty input stands for null, as the API saves blank values. */
const emptyAsNull = {
  setValueAs: (value: string) => (value === '' ? null : value),
}

/**
 * Edits the profile apps receive when the user opens them; failures show under the fields. The fields follow the
 * profile as it is refetched, except those the user has changed.
 */
export function ProfileForm() {
  const { t } = useTranslation()
  const { data: profile } = useGetApiProfile()
  const form = useForm<Profile>({
    resolver: zodResolver(PutApiProfileBody),
    values: profile,
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
      toast.success(t('profile.saved'))
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
          <FieldLegend>{t('profile.address')}</FieldLegend>
          {addressInputs.map((input) => (
            <ProfileInput key={input.name} form={form} {...input} />
          ))}
        </FieldSet>
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {t('profile.save')}
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
  const { t } = useTranslation()
  const { error } = form.getFieldState(name, form.formState)

  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={name}>{t(`profile.${label}`)}</FieldLabel>
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
