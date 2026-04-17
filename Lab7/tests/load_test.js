import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 50 },  // ramp up to 50 VU
    { duration: '4m', target: 50 },   // stay at 50 VU for 4 minutes
    { duration: '30s', target: 0 },   // ramp down to 0
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should complete within 500ms
    http_req_failed: ['rate<0.01'],   // error rate should be less than 1%
  },
};

export default function () {
  // Mix of different endpoints
  const endpoint = Math.random();
  let res;
  
  if (endpoint < 0.5) {
    // GET /api/students
    res = http.get('http://localhost:5000/api/students');
  } else if (endpoint < 0.75) {
    // GET /api/students/{id}
    const studentId = Math.floor(Math.random() * 10000) + 1;
    res = http.get(`http://localhost:5000/api/students/${studentId}`);
  } else {
    // GET /api/students/search?q=term
    const searchTerms = ['Alice', 'Bob', 'Charlie', 'Diana', 'Eve'];
    const term = searchTerms[Math.floor(Math.random() * searchTerms.length)];
    res = http.get(`http://localhost:5000/api/students/search?q=${term}`);
  }
  
  check(res, {
    'status is 200 or 404': (r) => r.status === 200 || r.status === 404,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
  
  sleep(1);
}
