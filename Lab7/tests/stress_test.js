import http from 'k6/http';
import { check } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 50 },    // ramp up to 50 VU
    { duration: '2m', target: 100 },   // ramp up to 100 VU
    { duration: '3m', target: 200 },   // ramp up to 200 VU
    { duration: '2m', target: 100 },   // ramp down to 100 VU
    { duration: '1m', target: 0 },     // ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(99)<2000'], // 99% of requests < 2s
    http_req_failed: ['rate<0.1'],     // error rate < 10%
  },
};

const endpoints = [
  'http://localhost:5000/api/students',
  'http://localhost:5000/api/students/1',
  'http://localhost:5000/api/students/search?q=test',
];

export default function () {
  const endpoint = endpoints[Math.floor(Math.random() * endpoints.length)];
  const res = http.get(endpoint);
  
  check(res, {
    'response status 200 or 404': (r) => r.status === 200 || r.status === 404,
    'response time < 2000ms': (r) => r.timings.duration < 2000,
  });
}
