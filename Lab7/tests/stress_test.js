import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '2m', target: 10 },   // 10 RPS for 2 min
    { duration: '2m', target: 50 },   // 50 RPS for 2 min
    { duration: '2m', target: 100 },  // 100 RPS for 2 min
    { duration: '2m', target: 250 },  // 250 RPS for 2 min
    { duration: '2m', target: 500 },  // 500 RPS for 2 min
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],   // error rate should stay below 5%
  },
};

export default function () {
  const res = http.get('http://localhost:5000/api/students');
  
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  
  // No sleep - we want to stress test
}
