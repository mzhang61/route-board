import { App } from '@modelcontextprotocol/ext-apps';
import { createRoot } from 'react-dom/client';
import { useEffect, useState } from 'react';
import { z } from 'zod';
import { tripSchema } from './api';
import './styles.css';

const previewSchema = z.object({
  current: tripSchema,
  proposed: z.object({ version: z.number(), kind: z.string(), delayMinutes: z.number(), stopOrder: z.number().nullable() }),
  confirmationToken: z.string(), expiresAt: z.string(), notice: z.string(),
});
type Preview = z.infer<typeof previewSchema>;
const bridge = new App({ name: 'RouteBoard confirmation', version: '1.0.0' });

function ConfirmationApp() {
  const [preview, setPreview] = useState<Preview | null>(null);
  const [message, setMessage] = useState('Waiting for a proposed trip update…');
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    bridge.ontoolresult = result => {
      try {
        const data: unknown = result.structuredContent
          ?? JSON.parse(result.content?.find(item => item.type === 'text')?.text ?? 'null');
        setPreview(previewSchema.parse(data)); setMessage('Review the exact change below before confirming.');
      } catch { setPreview(null); setMessage('The host did not supply a valid update preview. Request a new preview.'); }
    };
    void bridge.connect().catch(() => setMessage('Could not connect to the MCP host. Reopen this App from a preview tool.'));
    return () => { void bridge.close(); };
  }, []);

  async function confirm() {
    if (!preview) return;
    setBusy(true);
    try {
      const result = await bridge.callServerTool({ name: 'record_trip_update', arguments: {
        confirmationToken: preview.confirmationToken, confirmed: true,
      } });
      if (result.isError) throw new Error('The update was rejected. Request a fresh preview and check permissions.');
      setPreview(null); setMessage('The confirmed update was saved.');
    } catch (error) { setMessage(error instanceof Error ? error.message : 'The update failed.'); }
    finally { setBusy(false); }
  }
  return <main><h1>Confirm synthetic trip update</h1><p role="status">{message}</p>
    {preview && <section><h2>Trip {preview.current.id}: {preview.current.routeName}</h2>
      <p>Vehicle: {preview.current.vehicleCode}. Version: {preview.proposed.version}.</p>
      <ol>{preview.current.stops.map(stop => <li key={stop.order}>{stop.name}: scheduled {stop.scheduled}, estimated {stop.estimated}, simulated observed {stop.simulatedObserved ?? 'not observed'}</li>)}</ol>
      <p>Proposed action: <strong>{preview.proposed.kind}</strong>. Total delay: {preview.proposed.delayMinutes} minutes. Stop: {preview.proposed.stopOrder ?? 'not applicable'}.</p>
      <p>Expires: {preview.expiresAt}. The server rechecks role, scope, actor and version.</p>
      <button disabled={busy || Date.parse(preview.expiresAt) <= Date.now()} onClick={() => void confirm()}>{busy ? 'Saving…' : 'Confirm this exact update'}</button>
      <button disabled={busy} onClick={() => { setPreview(null); setMessage('Canceled. No trip update was sent.'); }}>Cancel</button>
    </section>}
  </main>;
}
const root = document.getElementById('root');
if (root) createRoot(root).render(<ConfirmationApp />);
