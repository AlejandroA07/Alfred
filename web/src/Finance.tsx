import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import MoneyMap, { type MoneyMapData } from './MoneyMap'
import './Finance.css'

type Category = { id: string; name: string; color: string; monthlyBudget: number | null }
type Expense = { id: string; categoryId: string; amount: number; date: string; note: string | null }

const currency = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'EUR' })
const dayLabel = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short' })

function currentMonth(): string {
  return new Date().toISOString().slice(0, 7)
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

/** Pulls a human-readable message out of an ASP.NET ProblemDetails payload. */
async function problemText(res: Response, fallback: string): Promise<string> {
  const problem = (await res.json().catch(() => null)) as { errors?: Record<string, string[]> } | null
  return problem?.errors ? Object.values(problem.errors).flat().join(' ') : fallback
}

export default function Finance({ email, onLogout }: { email: string; onLogout: () => void }) {
  const [categories, setCategories] = useState<Category[]>([])
  const [expenses, setExpenses] = useState<Expense[]>([])
  const [moneyMap, setMoneyMap] = useState<MoneyMapData | null>(null)
  const [month, setMonth] = useState(currentMonth())

  const [categoryId, setCategoryId] = useState('')
  const [amount, setAmount] = useState('')
  const [date, setDate] = useState(today())
  const [note, setNote] = useState('')

  const [newCategoryName, setNewCategoryName] = useState('')
  const [newCategoryColor, setNewCategoryColor] = useState('#3a6ea5')

  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const loadCategories = useCallback(async () => {
    const res = await fetch('/api/finance/categories', { credentials: 'include' })
    if (res.ok) setCategories((await res.json()) as Category[])
  }, [])

  const loadExpenses = useCallback(async () => {
    const res = await fetch(`/api/finance/expenses?month=${month}`, { credentials: 'include' })
    if (res.ok) setExpenses((await res.json()) as Expense[])
  }, [month])

  const loadMoneyMap = useCallback(async () => {
    const res = await fetch(`/api/finance/money-map?month=${month}`, { credentials: 'include' })
    if (res.ok) setMoneyMap((await res.json()) as MoneyMapData)
  }, [month])

  useEffect(() => {
    void loadCategories()
  }, [loadCategories])

  useEffect(() => {
    void loadExpenses()
  }, [loadExpenses])

  useEffect(() => {
    void loadMoneyMap()
  }, [loadMoneyMap])

  // Keep the form's selected category valid as the category list loads/changes.
  useEffect(() => {
    if (categories.length > 0 && !categories.some((c) => c.id === categoryId)) {
      setCategoryId(categories[0].id)
    }
  }, [categories, categoryId])

  const categoriesById = useMemo(
    () => new Map(categories.map((c) => [c.id, c])),
    [categories],
  )

  const total = useMemo(() => expenses.reduce((sum, e) => sum + e.amount, 0), [expenses])

  async function addCategory(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await fetch('/api/finance/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ name: newCategoryName, color: newCategoryColor, monthlyBudget: null }),
      })
      if (!res.ok) {
        setError(await problemText(res, 'Could not add category.'))
        return
      }
      setNewCategoryName('')
      await Promise.all([loadCategories(), loadMoneyMap()])
    } finally {
      setBusy(false)
    }
  }

  async function logExpense(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await fetch('/api/finance/expenses', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          categoryId,
          amount: Number(amount),
          date,
          note: note.trim() === '' ? null : note.trim(),
        }),
      })
      if (!res.ok) {
        setError(await problemText(res, 'Could not log expense.'))
        return
      }
      setAmount('')
      setNote('')
      await Promise.all([loadExpenses(), loadMoneyMap()])
    } finally {
      setBusy(false)
    }
  }

  async function deleteExpense(id: string) {
    const res = await fetch(`/api/finance/expenses/${id}`, { method: 'DELETE', credentials: 'include' })
    if (res.ok) await Promise.all([loadExpenses(), loadMoneyMap()])
  }

  return (
    <main className="finance">
      <header className="finance-header">
        <div>
          <h1>Alfred</h1>
          <p className="tagline">Good to see you, {email}.</p>
        </div>
        <button type="button" className="link" onClick={onLogout}>
          Log out
        </button>
      </header>

      {error && <p className="error">{error}</p>}

      {categories.length === 0 ? (
        <form onSubmit={addCategory} className="card">
          <h2>Add your first category</h2>
          <p className="hint">Expenses are logged against a category. Create one to get started.</p>
          <label>
            Name
            <input
              value={newCategoryName}
              onChange={(e) => setNewCategoryName(e.target.value)}
              required
              maxLength={60}
              placeholder="Groceries"
            />
          </label>
          <label>
            Colour
            <input
              type="color"
              value={newCategoryColor}
              onChange={(e) => setNewCategoryColor(e.target.value)}
              className="color-input"
            />
          </label>
          <button type="submit" disabled={busy}>
            Add category
          </button>
        </form>
      ) : (
        <form onSubmit={logExpense} className="card">
          <h2>Log an expense</h2>
          <label>
            Category
            <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </label>
          <div className="row">
            <label>
              Amount
              <input
                type="number"
                inputMode="decimal"
                min="0.01"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                required
                placeholder="0.00"
              />
            </label>
            <label>
              Date
              <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
            </label>
          </div>
          <label>
            Note (optional)
            <input value={note} onChange={(e) => setNote(e.target.value)} maxLength={200} placeholder="What was it?" />
          </label>
          <button type="submit" disabled={busy}>
            Log expense
          </button>
        </form>
      )}

      <section className="card">
        <div className="month-bar">
          <label className="month-picker">
            Month
            <input type="month" value={month} onChange={(e) => setMonth(e.target.value)} />
          </label>
          <span className="total">{currency.format(total)}</span>
        </div>

        {moneyMap && <MoneyMap data={moneyMap} />}

        {expenses.length === 0 ? (
          <p className="hint">No expenses logged for this month.</p>
        ) : (
          <ul className="expense-list">
            {expenses.map((expense) => {
              const category = categoriesById.get(expense.categoryId)
              return (
                <li key={expense.id} className="expense-row">
                  <span className="swatch" style={{ background: category?.color ?? '#888' }} aria-hidden />
                  <span className="expense-main">
                    <span className="expense-category">{category?.name ?? 'Unknown'}</span>
                    {expense.note && <span className="expense-note">{expense.note}</span>}
                  </span>
                  <span className="expense-date">{dayLabel.format(new Date(expense.date))}</span>
                  <span className="expense-amount">{currency.format(expense.amount)}</span>
                  <button
                    type="button"
                    className="delete"
                    aria-label="Delete expense"
                    onClick={() => deleteExpense(expense.id)}
                  >
                    ×
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </section>
    </main>
  )
}
