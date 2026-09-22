type Props = {
  eyebrow: string
  title: string
  description: string
  endpoints: string[]
}

export function ModulePlaceholder({ eyebrow, title, description, endpoints }: Props) {
  return (
    <section className="page-stack">
      <header className="page-heading">
        <span className="eyebrow">{eyebrow}</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </header>
      <div className="panel module-contract">
        <h2>API boundary đã chốt</h2>
        {endpoints.map((endpoint) => <code key={endpoint}>{endpoint}</code>)}
        <p>Contract chi tiết nằm trong MODULE_API_PLAN.md. Feature này có route riêng để triển khai mà không làm phình module khác.</p>
      </div>
    </section>
  )
}
