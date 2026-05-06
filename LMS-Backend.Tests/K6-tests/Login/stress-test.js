import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

// Custom metrics
const loginDuration = new Trend('login_duration');
const loginFailRate = new Rate('login_fail_rate');

// Test configuration
export const options = {
  stages: [
    { duration: '20s', target: 20 },   // warm up
    { duration: '20s', target: 50 },   // moderate load
    { duration: '20s', target: 100 },  // high load
    { duration: '20s', target: 150 },  // stress peak
    { duration: '10s', target: 0 },    // ramp down
  ],

  thresholds: {
    http_req_duration: ['p(95)<3500'],  // 95% under 3s
    http_req_failed: ['rate<0.20'],     // less than 20% failures
  },
};

const BASE_URL = 'http://localhost:5251';

export default function () {

  const payload = JSON.stringify({
    email: 'admin@lms.local',
    password: 'Admin123!',
    deviceId: `k6-device-${__VU}-${__ITER}`,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    timeout: '10s',
  };

  const start = new Date().getTime();

  const res = http.post(`${BASE_URL}/api/v1/auth/login`, payload, params);

  const duration = new Date().getTime() - start;
  loginDuration.add(duration);

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'has access token': (r) => {
      try {
        return r.json('accessToken') !== undefined;
      } catch (e) {
        return false;
      }
    },
    'response < 3s': (r) => r.timings.duration < 3000,
  });

  loginFailRate.add(!success);

  // Optional small think time (simulates real users)
  sleep(0.5);
}