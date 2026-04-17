import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 1 }, // 1 virtual user for 1 minute
  ],
  thresholds: {
    http_req_duration: ['p(95)<200'], // 95% of requests should complete within 200ms
    http_req_failed: ['rate<0.01'],   // error rate should be less than 1%
  },
};

export default function () {
  const res = http.get('http://localhost:5000/api/students');
  
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 200ms': (r) => r.timings.duration < 200,
    'has students': (r) => r.body.includes('FirstName'),
  });
  
  sleep(1);
}
