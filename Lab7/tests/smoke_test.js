import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 5 },   // ramp up to 5 VU
    { duration: '1m30s', target: 5 }, // stay at 5 VU
    { duration: '30s', target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.05'],
    checks: ['rate>0.95'],
  },
};

export default function () {
  // Test GET /api/students
  let res = http.get('http://localhost:5000/api/students');
  check(res, {
    'GET /api/students status 200': (r) => r.status === 200,
    'GET /api/students response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);

  // Test GET /api/students/{id}
  res = http.get('http://localhost:5000/api/students/1');
  check(res, {
    'GET /api/students/{id} status 200 or 404': (r) => r.status === 200 || r.status === 404,
    'GET /api/students/{id} response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);

  // Test search endpoint
  res = http.get('http://localhost:5000/api/students/search?q=John');
  check(res, {
    'GET /api/students/search status 200': (r) => r.status === 200,
    'GET /api/students/search response time < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}
