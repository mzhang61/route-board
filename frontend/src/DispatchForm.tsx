import { useState } from 'react';
import type { FormEvent } from 'react';
import { accessToken } from './auth';
import { request, tripSchema } from './api';
import type { Catalog, Trip } from './api';

export function DispatchForm({ catalog, selected, onSaved }: {
  catalog: Catalog; selected: Trip | null; onSaved: () => void;
}) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true); setMessage('');
    try {
      const token = await accessToken();
      const body = selected ? {
        version: selected.version, kind: form.get('kind'), delayMinutes: Number(form.get('delay')),
        stopOrder: form.get('stop') ? Number(form.get('stop')) : null,
      } : {
        routeId: Number(form.get('route')), vehicleId: Number(form.get('vehicle')),
        scheduledDeparture: new Date(String(form.get('departure'))).toISOString(),
      };
      await request(selected ? `/api/trips/${selected.id}/updates` : '/api/trips', tripSchema, {
        method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify(body),
      });
      setMessage('Saved. The board will refresh.'); onSaved();
    } catch (error) { setMessage(error instanceof Error ? error.message : 'The update failed.'); }
    finally { setBusy(false); }
  }
  return <section aria-labelledby="dispatch-heading">
    <h2 id="dispatch-heading">{selected ? `Update trip ${selected.id}` : 'Create a synthetic trip'}</h2>
    <p>Dispatcher role and write scope are checked by the server.</p>
    <form onSubmit={submit}>
      {selected ? <>
        <label>Update<select name="kind"><option value="delay">Set total delay</option><option value="depart">Record simulated departure</option><option value="arrive">Record simulated stop arrival</option><option value="cancel">Cancel trip</option></select></label>
        <label>Total delay (minutes)<input name="delay" type="number" min="0" max="1440" defaultValue={selected.delayMinutes} required /></label>
        <label>Stop for arrival<select name="stop"><option value="">Select a stop</option>{selected.stops.map(stop => <option key={stop.order} value={stop.order}>{stop.order}. {stop.name}</option>)}</select></label>
      </> : <>
        <label>Route<select name="route" required>{catalog.routes.map(route => <option key={route.id} value={route.id}>{route.name}</option>)}</select></label>
        <label>Synthetic vehicle<select name="vehicle" required>{catalog.vehicles.map(vehicle => <option key={vehicle.id} value={vehicle.id}>{vehicle.code}</option>)}</select></label>
        <label>Departure (your local time)<input name="departure" type="datetime-local" required /></label>
      </>}
      <button disabled={busy || catalog.routes.length === 0}>{busy ? 'Saving…' : 'Save trip'}</button>
    </form>
    <p role="status">{message}</p>
  </section>;
}
