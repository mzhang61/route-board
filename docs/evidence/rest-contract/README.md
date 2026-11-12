# REST contract

Tested commit `4bfb149f1369518c0b6e57ab1d7d854c132f21f8`. Follow the repository's local setup and run this command from the repository root with curl available.

```sh
ROUTEBOARD_HTTP_EVIDENCE=/tmp/routeboard-http.txt bash scripts/test.sh
```

[Raw HTTP responses](curl.txt) came from curl over a loopback connection to Kestrel with a disposable PostGIS database. The command above generates fresh ports, dates and trace IDs. The test supplies synthetic signed credentials through standard input. The response capture contains no bearer credentials. Real provider login has separate evidence.

The expected outcomes were encoded as assertions before the run. Creating a trip returned 201 and its Location yielded the saved trip. Malformed JSON produced 400, anonymous creation produced 401, and a viewer received 403. A missing trip produced 404. Two dispatch updates used the same version and one succeeded while the other received 409. Error responses carried Problem Details with a matching status and a title. The valid preflight returned 204. The SSE client deliberately disconnected after two seconds.

The full run passed 39 backend tests and two browser tests. AI authored the test and captured its output. Reviewing the actual route behavior clarified the 204 claim: it belongs to the browser preflight. The transcript records application updates returning 200.
