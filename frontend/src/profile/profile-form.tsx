import { zodResolver } from '@hookform/resolvers/zod'
import { PencilIcon } from 'lucide-react'
import { useState } from 'react'
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
  fullWidth?: boolean
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
    fullWidth: true,
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
 * Shows the profile apps receive when the user opens them, editable after pressing Edit; failures show under the
 * fields. The fields follow the profile as it is refetched.
 */
export function ProfileForm() {
  const { t } = useTranslation()
  const [isEditing, setIsEditing] = useState(false)
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
      setIsEditing(false)
      toast.success(t('profile.saved'))
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
          <CardTitle>{t('profile.personalInformation')}</CardTitle>
          <CardDescription>{t('profile.description')}</CardDescription>
          {!isEditing && (
            <CardAction>
              <Button type="button" variant="outline" onClick={edit}>
                <PencilIcon />
                {t('profile.edit')}
              </Button>
            </CardAction>
          )}
        </CardHeader>
        <CardContent>
          <FieldGroup>
            <ProfileFieldSet
              legend={t('profile.aboutYou')}
              inputs={personalInputs}
              form={form}
              readOnly={!isEditing}
            />
            <FieldSeparator />
            <ProfileFieldSet
              legend={t('profile.address')}
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
              {t('profile.cancel')}
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {t('profile.save')}
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
  const { t } = useTranslation()
  const { error } = form.getFieldState(name, form.formState)

  return (
    <Field data-invalid={!!error} className={cn(fullWidth && 'sm:col-span-2')}>
      <FieldLabel htmlFor={name}>{t(`profile.${label}`)}</FieldLabel>
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
