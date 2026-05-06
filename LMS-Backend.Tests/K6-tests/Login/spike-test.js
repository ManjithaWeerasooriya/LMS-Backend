import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Metrics
const loginDuration = new Trend('login_duration');
const loginFailRate = new Rate('login_fail_rate');
const httpFailRate = new Rate('http_fail_rate');

export const options = {
  stages: [
    { duration: '10s', target: 10 },   // warm-up
    { duration: '10s', target: 30 },   // normal load
    { duration: '10s', target: 100 },  // SPIKE
    { duration: '15s', target: 100 },  // sustain spike
    { duration: '10s', target: 0 },    // ramp down
  ],

  thresholds: {
    http_req_duration: ['p(95)<6000'],
    http_req_failed: ['rate<0.20'],
    login_fail_rate: ['rate<0.20'],
  },
};

const BASE_URL = 'http://localhost:5251';

export default function () {

  const payload = JSON.stringify({
    email: 'admin@lms.local',
    password: 'Admin123!',
    deviceId: `spike-vu${__VU}-iter${__ITER}`,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
    timeout: '30s',
  };

  const res = http.post(`${BASE_URL}/api/v1/auth/login`, payload, params);

  // Measure duration
  loginDuration.add(res.timings.duration);

  // Track HTTP failures
  const isSuccess = check(res, {
    'status is 200': (r) => r.status === 200,
    'has access token': (r) => {
      try {
        const body = r.json();
        return body && body.accessToken;
      } catch {
        return false;
      }
    },
    'response time OK': (r) => r.timings.duration < 6000,
  });

  loginFailRate.add(!isSuccess);
  httpFailRate.add(res.status !== 200);

  // Debug only failures
  if (res.status !== 200) {
    console.log(`❌ FAIL | VU=${__VU} ITER=${__ITER} STATUS=${res.status} TIME=${res.timings.duration}ms`);
  }

  sleep(0.2);
}