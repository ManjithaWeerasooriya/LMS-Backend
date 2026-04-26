import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const sessionsDuration = new Trend('sessions_duration');
const sessionsFailRate = new Rate('sessions_fail_rate');

const COURSE_ID = '307e455e-3990-4919-b9e4-0e89e0939860';

export const options = {
  stages: [
    { duration: '15s', target: 0   }, // baseline
    { duration: '5s',  target: 100 }, // spike
    { duration: '20s', target: 100 }, // hold
    { duration: '5s',  target: 0   }, // recovery
  ],
  thresholds: {
    http_req_duration:  ['p(95)<6000'],
    http_req_failed:    ['rate<0.10'],
    sessions_fail_rate: ['rate<0.10'],
  },
};

const BASE_URL = 'http://localhost:5251';

export function setup() {
  const res = http.post(`${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({
      email:    'inuransira@gmail.com',
      password: 'Google@2009',
      deviceId: 'k6-sessions-spike-setup',
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  const token = res.json('accessToken');
  if (!token) throw new Error(`Login failed: ${res.body}`);
  return { token };
}

export default function (data) {
  const params = {
    headers: {
      'Content-Type':  'application/json',
      'Authorization': `Bearer ${data.token}`,
    },
  };

  const res = http.get(
    `${BASE_URL}/api/v1/student/courses/${COURSE_ID}/live-sessions`,
    params
  );

  sessionsDuration.add(res.timings.duration);
  sessionsFailRate.add(res.status !== 200);

  check(res, {
    'status is 200':        (r) => r.status === 200,
    'has sessions data':    (r) => r.json('data') !== null,
    'response time < 5s':   (r) => r.timings.duration < 5000,
  });

  sleep(1);
}