import { useState, type FormEvent } from 'react'
import { problemText } from './problem'
import './Categories.css'

export type Category = { id: string; name: string; color: string; monthlyBudget: number | null }

/** Turns a budget input into the API's `decimal | null` — a blank field means "no budget". */
function parseBudget(value: string): number | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : Number(trimmed)
}

export default function Categories({
  categories,
  onChanged,
}: {
  categories: Category[]
  onChanged: () => Promise<void>
}) {
  const [name, setName] = useState('')
  const [color, setColor] = useState('#3a6ea5')
  const [budget, setBudget] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function add(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await fetch('/api/finance/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ name, color, monthlyBudget: parseBudget(budget) }),
      })
      if (!res.ok) {
        setError(await problemText(res, 'Could not add category.'))
        return
      }
      setName('')
      setBudget('')
      await onChanged()
    } finally {
      setBusy(false)
    }
  }

  // PUT replaces the whole category, so a budget edit resends the current name and colour.
  async function saveBudget(category: Category, monthlyBudget: number | null) {
    setError(null)
    const res = await fetch(`/api/finance/categories/${category.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ name: category.name, color: category.color, monthlyBudget }),
    })
    if (!res.ok) {
      setError(await problemText(res, 'Could not update budget.'))
      return
    }
    await onChanged()
  }

  return (
    <section className="card categories">
      <h2>Categories</h2>
      {error && <p className="error">{error}</p>}

      {categories.length === 0 ? (
        <p className="hint">Expenses are logged against a category. Create one to get started.</p>
      ) : (
        <ul className="category-list">
          {categories.map((c) => (
            <CategoryRow key={c.id} category={c} onSave={saveBudget} />
          ))}
        </ul>
      )}

      <form onSubmit={add} className="category-add">
        <div className="row">
          <label className="grow">
            Name
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              maxLength={60}
              placeholder="Groceries"
            />
          </label>
          <label className="color-field">
            Colour
            <input
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              className="color-input"
            />
          </label>
        </div>
        <label>
          Monthly budget (optional)
          <input
            type="number"
            inputMode="decimal"
            min="0"
            step="0.01"
            value={budget}
            onChange={(e) => setBudget(e.target.value)}
            placeholder="e.g. 300"
          />
        </label>
        <button type="submit" disabled={busy}>
          Add category
        </button>
      </form>
    </section>
  )
}

function CategoryRow({
  category,
  onSave,
}: {
  category: Category
  onSave: (category: Category, monthlyBudget: number | null) => Promise<void>
}) {
  const [value, setValue] = useState(category.monthlyBudget === null ? '' : String(category.monthlyBudget))
  const [busy, setBusy] = useState(false)

  const parsed = parseBudget(value)
  const invalid = parsed !== null && (Number.isNaN(parsed) || parsed < 0)
  const dirty = parsed !== category.monthlyBudget

  async function save() {
    if (invalid || !dirty) return
    setBusy(true)
    try {
      await onSave(category, parsed)
    } finally {
      setBusy(false)
    }
  }

  return (
    <li className="category-row">
      <span className="swatch" style={{ background: category.color }} aria-hidden />
      <span className="category-name">{category.name}</span>
      <input
        type="number"
        inputMode="decimal"
        min="0"
        step="0.01"
        className="budget-input"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder="No budget"
        aria-label={`Monthly budget for ${category.name}`}
      />
      <button type="button" className="save" disabled={busy || invalid || !dirty} onClick={() => void save()}>
        Save
      </button>
    </li>
  )
}
