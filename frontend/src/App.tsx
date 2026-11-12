import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { createPortal } from 'react-dom';
import { auth } from './auth';
import { boardSchema, catalogSchema, estimateSchema, request, tripSchema } from './api';
import type { Catalog, Trip } from './api';
import { Positions } from './Positions';
import { DispatchForm } from './DispatchForm';

function time(value: string): string { return new Date(value).toLocaleString(); }

export function App({ headerControls }: { headerControls: HTMLElement }) {
  const [catalog, setCatalog] = useState<Catalog | null>(null);
  const [catalogAttempt, setCatalogAttempt] = useState(0);
  const [catalogError, setCatalogError] = useState('');
  const [trips, setTrips] = useState<Trip[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [query, setQuery] = useState('');
  const [revision, setRevision] = useState(0);
  const [connection, setConnection] = useState('Connecting to live updates…');
  const [selected, setSelected] = useState<Trip | null>(null);
  const [estimate, setEstimate] = useState('');
  const [authMessage, setAuthMessage] = useState('');
  const [signedIn, setSignedIn] = useState(false);
  const [total, setTotal] = useState(0);
  const cursor = useRef(0);

  useEffect(() => {
    const controller = new AbortController();
    setCatalogError('');
    request('/api/catalog', catalogSchema, { signal: controller.signal }).then(setCatalog)
      .catch(() => { if (!controller.signal.aborted) setCatalogError('The route catalog could not be loaded.'); });
    return () => controller.abort();
  }, [catalogAttempt]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError('');
    request(`/api/trips?${query}`, boardSchema, { signal: controller.signal })
      .then(board => { setTrips(board.items); setTotal(board.total); })
      .catch(() => { if (!controller.signal.aborted) setError('The trip board could not be loaded. Try again.'); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [query, revision]);

  useEffect(() => {
    const stream = new EventSource('/api/events');
    stream.onopen = () => setConnection('Live updates connected');
    stream.onerror = () => setConnection('Live updates disconnected. Reconnecting automatically…');
    stream.addEventListener('trip', (event: MessageEvent<string>) => {
      const id = Number(event.lastEventId);
      if (!Number.isSafeInteger(id) || id <= cursor.current) return;
      try {
        tripSchema.parse(JSON.parse(event.data));
        cursor.current = id;
        // Reload a full snapshot instead of adding delays from individual messages.
        setRevision(value => value + 1);
      } catch { setConnection('A live update was invalid. Refresh the board.'); }
    });
    return () => stream.close();
  }, []);

  useEffect(() => {
    if (!auth) return;
    const action = window.location.pathname === '/auth/callback'
      ? auth.signinRedirectCallback().then(user => { window.history.replaceState({}, '', '/'); return user; })
      : auth.getUser();
    action.then(user => setSignedIn(Boolean(user && !user.expired)))
      .catch(() => setAuthMessage('Sign-in failed. Please retry.'));
  }, []);

  useEffect(() => {
    if (!selected) { setEstimate(''); return; }
    const controller = new AbortController();
    setEstimate('Loading external route reference…');
    request(`/api/routes/${selected.routeId}/estimate`, estimateSchema, { signal: controller.signal })
      .then(result => setEstimate(result.seconds === null ? 'External route reference unavailable. Schedules remain available.'
        : `${result.status}: ${Math.round(result.seconds / 60)} minutes, retrieved ${time(result.retrievedAt!)}. ${result.source}`))
      .catch(() => { if (!controller.signal.aborted) setEstimate('External route reference could not be loaded. Schedules remain available.'); });
    return () => controller.abort();
  }, [selected?.routeId]);

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const values = new FormData(event.currentTarget);
    const parameters = new URLSearchParams();
    for (const [key, value] of values) if (typeof value === 'string' && value) {
      parameters.set(key, key === 'from' || key === 'to' ? new Date(value).toISOString() : value);
    }
    setQuery(parameters.toString());
  }

  return <>
    {createPortal(<>
      <p role="status">{connection}</p>
      {auth ? <button onClick={() => {
        if (!auth) return;
        setAuthMessage('Opening sign-in…');
        void auth.signinRedirect({ nonce: crypto.randomUUID() }).catch(() => setAuthMessage('Could not open sign-in. Please retry.'));
      }}>Sign in</button> : <p>Dispatcher sign-in is not configured in this environment.</p>}
      <p role="status">{authMessage}</p>
    </>, headerControls)}
    <section aria-labelledby="board-heading"><h2 id="board-heading">Trip board</h2>
      {!catalog && !catalogError && <p role="status">Loading route catalog…</p>}
      {catalogError && <p role="alert">{catalogError}{' '}
        <button onClick={() => setCatalogAttempt(value => value + 1)}>Retry catalog</button>
      </p>}
      {catalog && catalog.routes.length === 0 && <p>No routes are available in the catalog.</p>}
      <form onSubmit={search} className="filters">
        <label>Search route or vehicle<input name="search" maxLength={100} type="search" /></label>
        <label>Route<select name="routeId"><option value="">All routes</option>{catalog?.routes.map(route => <option key={route.id} value={route.id}>{route.name}</option>)}</select></label>
        <label>Stop<select name="stopId"><option value="">All stops</option>{catalog?.stops.map(stop => <option key={stop.id} value={stop.id}>{stop.name}</option>)}</select></label>
        <label>Status<select name="status"><option value="">All statuses</option>{['scheduled', 'running', 'delayed', 'completed', 'canceled'].map(status => <option key={status}>{status}</option>)}</select></label>
        <label>Departing after<input name="from" type="datetime-local" /></label>
        <label>Departing before<input name="to" type="datetime-local" /></label>
        <button>Search trips</button>
      </form>
      {loading && <p role="status">Loading trips…</p>}
      {error && <p role="alert">{error} <button onClick={() => setRevision(value => value + 1)}>Retry</button></p>}
      {!loading && !error && trips.length === 0 && <p>No trips match these filters.</p>}
      {!loading && !error && trips.length > 0 && <><p>Showing {trips.length} of {total} trips. Narrow the filters to find a specific trip.</p>
        <div className="table-scroll"><table><caption>Scheduled synthetic trips</caption><thead><tr><th>Route</th><th>Vehicle</th><th>Scheduled departure</th><th>Status</th><th>Details</th></tr></thead>
          <tbody>{trips.map(trip => <tr key={trip.id}><td>{trip.routeName}</td><td>{trip.vehicleCode}</td><td>{time(trip.scheduledDeparture)}</td><td>{trip.status}</td><td><button onClick={() => setSelected(trip)}>View trip {trip.id}</button></td></tr>)}</tbody>
        </table></div></>}
    </section>
    {selected && <section aria-labelledby="detail-heading"><h2 id="detail-heading">Trip {selected.id}: {selected.routeName}</h2>
      <p>Total recorded delay: {selected.delayMinutes} minutes. Viewing version {selected.version}; close and reopen to review newer changes.</p><p role="status">{estimate}</p>
      <ol>{selected.stops.map(stop => <li key={stop.order}><strong>{stop.name}</strong><br />Scheduled: {time(stop.scheduled)}<br />Estimated: {selected.status === 'canceled' ? 'Unavailable — trip canceled' : time(stop.estimated)}<br />Simulated observed: {stop.simulatedObserved ? time(stop.simulatedObserved) : 'Not observed'}</li>)}</ol>
      <button onClick={() => setSelected(null)}>Close trip</button>
    </section>}
    {signedIn && catalog && <DispatchForm key={`${selected?.id ?? 'new'}-${selected?.version ?? 0}`} catalog={catalog} selected={selected} onSaved={() => setRevision(value => value + 1)} />}
    <Positions />
    <footer>Reference routing by Geoapify. External reference times are separate from the fixed synthetic timetable.</footer>
  </>;
}
