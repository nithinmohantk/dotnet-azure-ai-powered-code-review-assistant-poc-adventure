import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  stages: [
    { duration: '2m', target: 10 }, // Ramp up to 10 users
    { duration: '5m', target: 10 }, // Stay at 10 users
    { duration: '2m', target: 50 }, // Ramp up to 50 users
    { duration: '5m', target: 50 }, // Stay at 50 users
    { duration: '2m', target: 100 }, // Ramp up to 100 users
    { duration: '5m', target: 100 }, // Stay at 100 users
    { duration: '2m', target: 0 }, // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    http_req_failed: ['rate<0.1'], // Error rate should be below 10%
    errors: ['rate<0.1'], // Custom error rate should be below 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost:8080';
const API_TOKEN = __ENV.API_TOKEN || 'test-token';

export function setup() {
  // Setup code - create test data if needed
  console.log('Starting load test...');
  return {
    createdReviews: [],
  };
}

export default function(data) {
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${API_TOKEN}`,
  };

  // Test 1: Health check
  const healthResponse = http.get(`${BASE_URL}/health`, { headers });
  const healthOk = check(healthResponse, {
    'health check status is 200': (r) => r.status === 200,
    'health check response time < 100ms': (r) => r.timings.duration < 100,
  });
  errorRate.add(!healthOk);

  // Test 2: Create code review
  const createReviewPayload = JSON.stringify({
    title: `Performance Test Review ${Date.now()}`,
    description: 'This is a performance test code review',
    repositoryUrl: 'https://github.com/test/performance-repo',
    branchName: 'feature/performance-test',
    commitHash: 'abc123def456',
    requestedBy: 'performance-test@example.com',
    priority: 'Medium',
  });

  const createResponse = http.post(`${BASE_URL}/api/codereviews`, createReviewPayload, { headers });
  const createOk = check(createResponse, {
    'create review status is 201': (r) => r.status === 201,
    'create review response time < 1000ms': (r) => r.timings.duration < 1000,
    'create review has ID': (r) => r.json('codeReviewId') !== undefined,
  });
  errorRate.add(!createOk);

  let reviewId = null;
  if (createOk) {
    reviewId = createResponse.json('codeReviewId');
    data.createdReviews.push(reviewId);
  }

  // Test 3: Get code review by ID
  if (reviewId) {
    const getResponse = http.get(`${BASE_URL}/api/codereviews/${reviewId}`, { headers });
    const getOk = check(getResponse, {
      'get review status is 200': (r) => r.status === 200,
      'get review response time < 500ms': (r) => r.timings.duration < 500,
      'get review has correct ID': (r) => r.json('id') === reviewId,
    });
    errorRate.add(!getOk);
  }

  // Test 4: Search code reviews
  const searchResponse = http.get(`${BASE_URL}/api/codereviews/search?searchTerm=performance&page=1&pageSize=20`, { headers });
  const searchOk = check(searchResponse, {
    'search reviews status is 200': (r) => r.status === 200,
    'search reviews response time < 800ms': (r) => r.timings.duration < 800,
    'search reviews returns array': (r) => Array.isArray(r.json()),
  });
  errorRate.add(!searchOk);

  // Test 5: Get code review metrics
  if (reviewId) {
    const metricsResponse = http.get(`${BASE_URL}/api/codereviews/${reviewId}/metrics`, { headers });
    const metricsOk = check(metricsResponse, {
      'get metrics status is 200': (r) => r.status === 200,
      'get metrics response time < 1000ms': (r) => r.timings.duration < 1000,
      'get metrics has data': (r) => r.json('totalFiles') !== undefined,
    });
    errorRate.add(!metricsOk);
  }

  // Test 6: Get user code reviews
  const userReviewsResponse = http.get(`${BASE_URL}/api/codereviews/user/performance-test@example.com?page=1&pageSize=20`, { headers });
  const userReviewsOk = check(userReviews复审Response, {
    'get user reviews status is 200': (r) => r.status === 200,
    'get user reviews response time < 600ms': (r) => r.timings.duration < 600,
    'get user reviews returns array': (r) => Array.isArray(r.json()),
  });
  errorRate.add(!userReviewsOk);

  sleep(1); // Wait between requests
}

export function teardown(data) {
  // Cleanup code - remove test data if needed
  console.log(`Created ${data.createdReviews.length} reviews during test`);
  console.log('Load test completed');
}

export function handleSummary(data) {
  // Custom summary reporting
  console.log('Load Test Summary:');
  console.log(`Total requests: ${dataesion.metrics.http_reqs.count}`);
  console.log(`Failed requests: ${data.metrics.http_req_failed.count}`);
  console.log(`Average response time: ${data.metrics.http_req_duration.avg}ms`);
  console.log(`95th percentile: ${data.metrics.http_req_duration['p(95)']}ms`);
  console.log(`99th percentile: ${data.metrics.http_req_duration['p(99)']}ms`);
}
