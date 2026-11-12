# Public DNS and delegation

Captured against repository revision `3145a44`. Cloudflare exported 13 configured records for mzcampuspulse.com, plus SOA and NS metadata. The [public copy](zone.redacted.txt) preserves every record row. Five values were withheld: two proxied Azure origin addresses and three verification TXT values. The [SHA-256 checksum](original.sha256) identifies the original download for private course submission.

The [two IPv4 delegation traces](delegation.txt) show the root, .com, the referral to amos.ns.cloudflare.com and romina.ns.cloudflare.com, and a final Cloudflare A answer for each environment. [Authoritative answers](authoritative.txt) include the aa flag, IPv4 and IPv6 edge addresses, and Let's Encrypt CAA records. The local resolver initially returned only 28 bytes with no chain. The complete captures came from the staging VM.

| Records | TTL and operational rationale |
| --- | --- |
| Production and staging A | Both are proxied with Auto TTL. The authority served the edge A and AAAA answers for 300 seconds. This limits how long clients reuse old edge addresses while avoiding a lookup on every visit. |
| Production and staging CAA | Auto, observed 300 seconds. A changed issuance policy can reach resolvers after a short cache interval. Both hosts authorize letsencrypt.org. |
| Apex, wildcard and www CNAME | Auto. These existing parking aliases retain the provider default so a target change can propagate without a custom long cache lifetime. |
| Two MX and SPF TXT | Auto. These existing forwarding and sender-policy entries use the provider default to accommodate changes to mail routing. |
| Two ACME TXT and the course verification TXT | Auto. Short caching accommodates challenge or verification changes. |
| Delegation and zone metadata | The .com referral served 172800 seconds. The exported child NS records specify 86400 seconds. Stable delegation benefits from longer caching. SOA metadata is managed by the DNS provider. |

The export uses 1 for Auto on these 13 records. For the application hosts the live TTL is directly visible in the authoritative capture. To reproduce from a host with outbound DNS access:

```sh
for name in staging routeboard; do
  dig -4 +trace +time=4 +tries=2 "$name.mzcampuspulse.com" A
  for type in A AAAA CAA; do
    dig @amos.ns.cloudflare.com +norecurse +noall +comments +answer "$name.mzcampuspulse.com" "$type"
  done
done
```

AI collected the DNS answers and downloaded the zone through the authenticated Cloudflare DNS Records export. The public traces contain public DNS responses. The original export remains local. Use Cloudflare DNS → Records → Export to obtain a fresh original.
