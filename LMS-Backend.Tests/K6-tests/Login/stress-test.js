import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const loginDuration = new Trend('login_duration');
const loginFailRate = new Rate('login_fail_rate');

export const options = {
  stages: [
    { duration: '20s', target: 20  }, // warm up
    { duration: '20s', target: 50  }, // moderate load
    { duration: '20s', target: 100 }, // high load
    { duration: '20s', target: 150 }, // beyond capacity — expect failures here
    { duration: '10s', target: 0   }, // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed:   ['rate<0.20'],
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
    'status is 200':       (r) => r.status === 200,
    'has accessToken':     (r) => r.json('accessToken') !== undefined,
    'response time < 3s':  (r) => r.timings.duration < 3000,
  });

  sleep(1);
}