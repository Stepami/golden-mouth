import http from 'k6/http';
import { check } from 'k6';

const baseUrl = (__ENV.BASE_URL || 'http://backend:8080').replace(/\/$/, '');
const initData = __ENV.INIT_DATA || '';
const worldId = __ENV.WORLD_ID || '';
const postId = __ENV.POST_ID || '';
const rate = Number(__ENV.RATE || 10);
const duration = __ENV.DURATION || '10m';
const durationMatch = /^(\d+)(s|m|h)$/.exec(duration);

if (!initData || !worldId || !postId) {
  throw new Error('Set INIT_DATA (without TMiniApp prefix), WORLD_ID and POST_ID. See load-tests/README.md.');
}
if (/^TMiniApp\s/i.test(initData)) {
  throw new Error('INIT_DATA must contain raw initData without the TMiniApp prefix.');
}
if (!Number.isInteger(rate) || rate < 1) {
  throw new Error('RATE must be a positive integer (requests/second per endpoint).');
}
if (!durationMatch || Number(durationMatch[1]) < 1) {
  throw new Error('DURATION must be a positive integer followed by s, m or h, for example 30s or 10m.');
}

const durationSeconds = Number(durationMatch[1]) * { s: 1, m: 60, h: 3600 }[durationMatch[2]];
const expectedRequests = rate * durationSeconds;
const query = `worldId=${encodeURIComponent(worldId)}`;
const paths = {
  profile: '/me',
  channel: `/posts/mine?${query}`,
  post: `/posts/${encodeURIComponent(postId)}?${query}`,
  feed: `/feed?${query}`,
};

const scenarios = {};
const thresholds = {};
for (const endpoint of Object.keys(paths)) {
  scenarios[endpoint] = {
    executor: 'constant-arrival-rate',
    exec: endpoint,
    rate,
    timeUnit: '1s',
    duration,
    preAllocatedVUs: Math.max(20, rate * 4),
    gracefulStop: '10s',
  };
  thresholds[`http_reqs{endpoint:${endpoint}}`] = [`count>=${expectedRequests}`];
  thresholds[`http_req_duration{endpoint:${endpoint}}`] = ['p(95)<500', 'p(99)<1000'];
  thresholds[`http_req_failed{endpoint:${endpoint}}`] = ['rate<0.01'];
  thresholds[`checks{endpoint:${endpoint}}`] = ['rate>0.99'];
  // Per-scenario thresholds also ensure dropped counts are visible in the report.
  thresholds[`dropped_iterations{scenario:${endpoint}}`] = ['count==0'];
}

export const options = {
  scenarios,
  thresholds,
  discardResponseBodies: true,
  setupTimeout: '30s',
  summaryTrendStats: ['avg', 'p(95)', 'p(99)', 'max'],
  // Do not use account IDs or URLs as metric labels.
  systemTags: ['status', 'method', 'name', 'scenario', 'expected_response', 'check', 'error_code'],
};

function get(endpoint, phase, responseType = 'none') {
  return http.get(`${baseUrl}${paths[endpoint]}`, {
    headers: { Authorization: `TMiniApp ${initData}` },
    tags: { endpoint: phase === 'setup' ? 'preflight' : endpoint, name: endpoint, phase },
    responseType,
    timeout: '3s',
    redirects: 0,
  });
}

function preflight(endpoint, validBody) {
  const response = get(endpoint, 'setup', 'text');
  if (response.status !== 200) {
    throw new Error(`Preflight ${endpoint}: expected HTTP 200, got ${response.status}. Check auth and data on this backend.`);
  }
  let body;
  try {
    body = response.json();
  } catch {
    throw new Error(`Preflight ${endpoint}: response is not JSON.`);
  }
  if (!validBody(body)) {
    throw new Error(`Preflight ${endpoint}: unexpected or empty test data. See load-tests/README.md.`);
  }
}

export function setup() {
  preflight('profile', body => typeof body?.id === 'string' && body.id.length > 0);
  preflight('channel', body => Array.isArray(body) && body.length > 0);
  preflight('post', body => body?.postId === postId && Array.isArray(body.comments) && body.comments.length > 0);
  // A feed read can schedule LLM generation: preserve the real application behavior.
  preflight('feed', body => Array.isArray(body) && body.length > 0);
  console.log(`Preflight passed. ${rate} RPS per endpoint, ${rate * 4} RPS total, ${duration}, ${expectedRequests} requests per endpoint.`);
  console.log('Feed reads can schedule LLM jobs. Monitor queue depth, waiting time and provider usage.');
}

function request(endpoint) {
  const response = get(endpoint, 'load');
  check(response, { 'HTTP 200': value => value.status === 200 }, { endpoint });
}

export function profile() { request('profile'); }
export function channel() { request('channel'); }
export function post() { request('post'); }
export function feed() { request('feed'); }

export function handleSummary(data) {
  const lines = [
    `Target: ${rate} RPS per endpoint for ${duration}; ${expectedRequests} requests per endpoint.`,
    'endpoint | requests | p95 ms | p99 ms | failed % | HTTP 200 % | dropped',
  ];
  for (const endpoint of Object.keys(paths)) {
    const values = metric => data.metrics[`${metric}{endpoint:${endpoint}}`]?.values;
    const format = value => Number.isFinite(value) ? value.toFixed(2) : 'n/a';
    const dropped = data.metrics[`dropped_iterations{scenario:${endpoint}}`]?.values.count;
    lines.push([
      endpoint,
      values('http_reqs')?.count ?? 0,
      format(values('http_req_duration')?.['p(95)']),
      format(values('http_req_duration')?.['p(99)']),
      format(values('http_req_failed')?.rate * 100),
      format(values('checks')?.rate * 100),
      dropped ?? 0,
    ].join(' | '));
  }

  const failures = [];
  for (const [name, metric] of Object.entries(data.metrics)) {
    for (const [threshold, result] of Object.entries(metric.thresholds || {})) {
      if (!result.ok) failures.push(`${name}: ${threshold}`);
    }
  }
  lines.push(failures.length ? `FAILED thresholds:\n${failures.join('\n')}` : 'All thresholds passed.');
  lines.push('Check that preflight completed and no iterations were interrupted. HTTP success alone does not prove LLM queue stability.');
  lines.push('Full aggregate report: load-tests/results/summary.json (overwritten on each run).');
  return {
    stdout: `${lines.join('\n')}\n`,
    [__ENV.SUMMARY_PATH || '/results/summary.json']: JSON.stringify(data, null, 2),
  };
}
