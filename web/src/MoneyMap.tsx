import './MoneyMap.css'

export type MoneyMapCategory = {
  categoryId: string
  name: string
  color: string
  monthlyBudget: number | null
  spent: number
}

export type MoneyMapData = {
  month: string
  totalSpent: number
  totalBudget: number | null
  categories: MoneyMapCategory[]
}

const currency = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'EUR' })
const percent = new Intl.NumberFormat(undefined, { style: 'percent', maximumFractionDigits: 0 })

/** Fraction of budget spent, clamped to [0, 1] for the bar width. */
function fill(spent: number, budget: number): number {
  if (budget <= 0) return spent > 0 ? 1 : 0
  return Math.min(spent / budget, 1)
}

export default function MoneyMap({ data }: { data: MoneyMapData }) {
  if (data.categories.length === 0) return null

  return (
    <div className="money-map">
      <ul className="money-map-list">
        {data.categories.map((c) => {
          const budget = c.monthlyBudget
          const over = budget !== null && c.spent > budget
          return (
            <li key={c.categoryId} className="money-map-row">
              <div className="money-map-top">
                <span className="money-map-name">
                  <span className="swatch" style={{ background: c.color }} aria-hidden />
                  {c.name}
                </span>
                <span className={`money-map-figures${over ? ' over' : ''}`}>
                  {currency.format(c.spent)}
                  {budget !== null && <span className="money-map-budget"> / {currency.format(budget)}</span>}
                </span>
              </div>
              {budget !== null ? (
                <div className="money-map-track" role="presentation">
                  <div
                    className={`money-map-bar${over ? ' over' : ''}`}
                    style={{ width: `${fill(c.spent, budget) * 100}%`, background: over ? undefined : c.color }}
                  />
                </div>
              ) : (
                <p className="money-map-untracked">No budget set</p>
              )}
              {budget !== null && (
                <p className="money-map-meta">
                  {over
                    ? `${currency.format(c.spent - budget)} over budget`
                    : `${percent.format(budget > 0 ? c.spent / budget : 0)} of budget · ${currency.format(budget - c.spent)} left`}
                </p>
              )}
            </li>
          )
        })}
      </ul>
    </div>
  )
}
