import { useEffect, useState } from 'react';
import { z } from 'zod';
import { request } from './api';

const schema = z.object({ source: z.string(), items: z.array(z.object({
  vehicleId: z.number(), tripId: z.number(), latitude: z.number(), longitude: z.number(),
  observedAt: z.string(), sequence: z.number(), isSynthetic: z.literal(true),
})) });
type PositionsResult = z.infer<typeof schema>;

export function Positions() {
  const [result, setResult] = useState<PositionsResult | null>(null);
  const [error, setError] = useState('');
  useEffect(() => {
    const controller = new AbortController();
    let timer: ReturnType<typeof setTimeout>;
    async function refresh() {
      try {
        setResult(await request('/api/positions', schema, { signal: controller.signal })); setError('');
      } catch { if (!controller.signal.aborted) setError('Synthetic positions unavailable. Retrying automatically.'); }
      finally { if (!controller.signal.aborted) timer = setTimeout(() => void refresh(), 5000); }
    }
    void refresh();
    return () => { controller.abort(); clearTimeout(timer); };
  }, []);
  return <section aria-labelledby="positions-heading"><h2 id="positions-heading">Synthetic position simulation</h2>
    <p>Linear interpolation between fixed stops. This is not real GPS or a road-following simulation.</p>
    {error && <p role="alert">{error}</p>}
    {!result && !error && <p role="status">Loading synthetic positions…</p>}
    {result?.items.length === 0 && <p>No simulated positions yet. A running trip and enabled simulator are required.</p>}
    {result && result.items.length > 0 && <ul>{result.items.map(position => <li key={position.vehicleId}>
      Vehicle {position.vehicleId}: {position.latitude.toFixed(5)}, {position.longitude.toFixed(5)}.
      {' '}Synthetic sample {position.sequence}, generated {new Date(position.observedAt).toLocaleString()}.
      {Date.now() - Date.parse(position.observedAt) > 15000 && ' Stale — no recent sample.'}
    </li>)}</ul>}
  </section>;
}
