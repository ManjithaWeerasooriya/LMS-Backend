import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const loginDuration = new Trend('login_duration');
const loginFailRate = new Rate('login_fail_rate');

export const options = {
  stages: [
    { duration: '15s', target: 0   }, // baseline — no load
    { duration: '5s',  target: 100 }, // spike — 0 to 100 VUs instantly
    { duration: '20s', target: 100 }, // hold the spike
    { duration: '5s',  target: 0   }, // recovery
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed:   ['rate<0.10'],
    login_fail_rate:   ['rate<0.10'],
  },
};

const BASE_URL = 'http://localhost:5251';

export default function () {
  const payload = JSON.stringify({
    email:    'inuransira@gmail.com',
    password: 'Google@2009!',
    deviceId: 'k6-spike-device',   // spike-test.js
    // deviceId: `k6-stress-device-${__VU}`,  // stress-test.js
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
  };

  const res = http.post(`${BASE_URL}/api/v1/auth/login`, payload, params);

  loginDuration.add(res.timings.duration);
  loginFailRate.add(res.status !== 200);

  check(res, {
    'status is 200':           (r) => r.status === 200,
    'has accessToken':         (r) => r.json('accessToken') !== undefined,
    'has refreshToken':        (r) => r.json('refreshToken') !== undefined,
    'response time < 2s':      (r) => r.timings.duration < 2000,
  });

  sleep(1);
}