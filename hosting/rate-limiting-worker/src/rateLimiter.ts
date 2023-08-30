import { MultiRegionRatelimit, Ratelimit } from '@upstash/ratelimit';
import { Redis } from '@upstash/redis/cloudflare';
import { Context } from 'vitest';

export interface RateLimitResult {
  success: boolean;
  retryAfterSeconds: number;
}

export class RateLimiterOptions {
  public constructor(
    public readonly redisUrl: string,
    public readonly redisToken: string
  ) {}
}

export class RateLimiter {
  public constructor(private readonly cache: Map<any, any>, private readonly options: RateLimiterOptions) {}

  public async apply(request: Request): Promise<RateLimitResult> {
    const redis = new Redis({
      url: this.options.redisUrl,
      token: this.options.redisToken,
    });

    const ratelimit = {
      api: new MultiRegionRatelimit({
        redis: [redis],
        limiter: MultiRegionRatelimit.slidingWindow(20, "60 s"),
        prefix: "ratelimit:api",
        ephemeralCache: this.cache,
      }),
      assets: new MultiRegionRatelimit({
        redis: [redis],
        limiter: MultiRegionRatelimit.slidingWindow(200, "60 s"),
        prefix: "ratelimit:assets",
        ephemeralCache: this.cache,
      })
    };

    let rateLimiter: MultiRegionRatelimit;
    if (request.method === "POST" || request.method === "DELETE" || request.method === "PUT" || request.method === "PATCH"
      || (request.method !== "OPTIONS" && new RegExp("^/([^/]*)/repositories").test(new URL(request.url).pathname))) {
      rateLimiter = ratelimit.api;
    }
    else {
      rateLimiter = ratelimit.assets;
    }

    const userIP: string = request.headers.get("CF-Connecting-IP") || "none";
    const response = await rateLimiter.limit(userIP);

    return {
      ...response,
      retryAfterSeconds: Math.ceil((response.reset - new Date().getTime()) / 1000),
    }
  }
}
