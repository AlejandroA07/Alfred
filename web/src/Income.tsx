import { useState, type FormEvent } from 'react'
import { problemText } from './problem'
import './Income.css'

export type Income = { id: string; amount: number; date: string; source: string }

const currency = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'EUR' })
const dayLabel = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short' })

export default function IncomeCard({
  incomes,
  defaultDate,
  onChanged,
}: {
  incomes: Income[]
  defaultDate: string
  onChanged: () => Promise<void>
}) {
  const [amount, setAmount] = useState('')
  const [source, setSource] = useState('')
  const [date, setDate] = useState(defaultDate)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function add(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await fetch('/api/finance/incomes', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ amount: Number(amount), date, source: source.trim() }),
      })
      if (!res.ok) {
        setError(await problemText(res, 'Could not add income.'))
        return
      }
      setAmount('')
      setSource('')
      await onChanged()
    } finally {
      setBusy(false)
    }
  }

  async function remove(id: string) {
    setError(null)
    const res = await fetch(`/api/finance/incomes/${id}`, { method: 'DELETE', credentials: 'include' })
    if (!res.ok) {
      setError(await problemText(res, 'Could not delete income.'))
      return
    }
    await onChanged()
  }

  return (
    <section className="card income">
      <h2>Income</h2>
      {error && <p className="error">{error}</p>}

      {incomes.length === 0 ? (
        <p className="hint">Add what came in this month — it's the top line of the money map.</p>
      ) : (
        <ul className="income-list">
          {incomes.map((income) => (
            <li key={income.id} className="income-row">
              <span className="income-source">{income.source}</span>
              <span className="income-date">{dayLabel.format(new Date(income.date))}</span>
              <span className="income-amount">{currency.format(income.amount)}</span>
              <button
                type="button"
                className="delete"
                aria-label={`Delete income from ${income.source}`}
                onClick={() => void remove(income.id)}
              >
                ×
              </button>
            </li>
          ))}
        </ul>
      )}

      <form onSubmit={add} className="income-add">
        <label>
          Source
          <input
            value={source}
            onChange={(e) => setSource(e.target.value)}
            required
            maxLength={60}
            placeholder="Salary"
          />
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
        <button type="submit" disabled={busy}>
          Add income
        </button>
      </form>
    </section>
  )
}
