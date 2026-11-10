import { z } from 'zod';

const stopSchema = z.object({
  stopId: z.number(), name: z.string(), order: z.number(),
  scheduled: z.string(), estimated: z.string(), simulatedObserved: z.string().nullable(),
});
export const tripSchema = z.object({
  id: z.number(), routeId: z.number(), routeName: z.string(), vehicleId: z.number(), vehicleCode: z.string(),
  status: z.string(), scheduledDeparture: z.string(), simulatedDeparture: z.string().nullable(),
  delayMinutes: z.number(), version: z.number(), isSynthetic: z.literal(true), stops: z.array(stopSchema),
});
export type Trip = z.infer<typeof tripSchema>;
export const boardSchema = z.object({ items: z.array(tripSchema), total: z.number(), page: z.number(), pageSize: z.number() });
const optionSchema = z.object({ id: z.number(), code: z.string(), name: z.string() });
export const catalogSchema = z.object({ routes: z.array(optionSchema), stops: z.array(optionSchema),
  vehicles: z.array(z.object({ id: z.number(), code: z.string(), capacity: z.number() })) });
export type Catalog = z.infer<typeof catalogSchema>;
export const estimateSchema = z.object({ status: z.string(), seconds: z.number().nullable(),
  meters: z.number().nullable(), retrievedAt: z.string().nullable(), source: z.string() });

export async function request<T>(url: string, schema: z.ZodType<T>, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (!response.ok) {
    if (response.status === 401) throw new Error('Your session is missing or expired. Sign in again.');
    if (response.status === 403) throw new Error('Your account does not have dispatcher permission.');
    if (response.status === 409) throw new Error('The trip changed or this update is not allowed. Refresh and review it.');
    throw new Error(`Request failed (${response.status}). Please try again.`);
  }
  return schema.parse(await response.json());
}
