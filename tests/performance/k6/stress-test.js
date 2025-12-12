import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Stress test configuration - high load for short duration
export const options = {
  stages: [
    { duration: '1m', target: 100 }, // Quick ramp up to 100 users
    { duration: '2m', target: 200 }, // Ramp up to 200 users
    { duration: '3m', target: 300 }, // Ramp up to 300 users
    { duration: '2m', target: 400 }, // Ramp up to 400 users
    { duration: '1m', target: 500 }, // Peak load at 500 users
    { duration: '2m', target: 500 }, // Sustain peak load
    { duration: '1m', target: 0 }, // Quick ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests should be below 2s under stress
    http_req_failed: ['rate<0.2'], // Error rate should be below 20% under stress
    errors: ['rate<0.2'], // Custom error rate should be below 20% under stress
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost:8080';
const API_TOKEN = __ENV.API_TOKEN || 'test-token';

export default function() {
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${API_TOKEN}`,
  };

  // Stress test with mixed workload
  
  // 30% Create operations
  if (Math.random() < 0.3) {
    const createReviewPayload = JSON.stringify({
      title: `Stress Test Review ${Date.now()}-${Math.random()}`,
      description: 'This is a stress test code review',
      repositoryUrl: 'https://github.com/test/stress-repo',
      branchName: 'feature/stress-test',
      commitHash: 'def456abc123',
      requestedBy: 'stress-test@example.com',
      priority: 'High',
    });

    const createResponse = http.post(`${BASE_URL}/api/codereviews`, createReviewPayload, { headers });
    const createOk = check(createResponse, {
      'create review status is 201': (r) => r.status === 201,
      'create review response time < 2000ms': (r) => r.timings.duration < 2000,
    });
    errorRate.add(!createOk);
  }

  // 40% Read operations
  if (Math.random() < 0.4) {
    const searchResponse = http.get(`${BASE_URL}/api/codereviews/search?searchTerm=test&page=1&pageSize=20`, { headers });
    const searchOk = check(searchResponse, {
      'search reviews status is 200': (r) => r.status === 200,
      'search reviews response time < 1500ms': (r) => r.timings.duration < 1500,
    });
    errorRate.add(!searchOk);
  }

  // 20% Metrics operations
  if (Math.random() < 0.2) {
    const metricsResponse = http.get(`${BASE_URL}/api/codereviews/3fa85f64-5717-4562-b3fc-2c963f66afa6/metrics`, { headers });
    const metricsOk = check(metricsResponse, {
      'get metrics status is 200 or 404': (r) => r.status === 200 || r.status === 404,
      'get metrics response time < 1000ms': (r) => r.timings.duration < 1000,
    });
    errorRate.add(!metricsOk);
  }

  // 10% Health check operations
  const healthResponse = http.get(`${BASE_URL}/health`, { headers });
  const healthOk = check(healthResponse, {
    'health check status is 200': (r) => r.status === 200,
    'health check response time < 500ms': (r) => r.timings.duration < 500,
  });
  errorRate.add(!healthOk);

  sleep(0.1); // Minimal wait between requests for stress testing
}
