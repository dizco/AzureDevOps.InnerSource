import { MultiRegionRatelimit } from '@upstash/ratelimit';
import { Redis } from '@upstash/redis/cloudflare';

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
  private readonly ratelimit: {api: MultiRegionRatelimit, assets: MultiRegionRatelimit};

  public constructor(cache: Map<any, any>, options: RateLimiterOptions) {
    const redis = new Redis({
      url: options.redisUrl,
      token: options.redisToken,
    });

    this.ratelimit = {
      api: new MultiRegionRatelimit({
        redis: [redis],
        limiter: MultiRegionRatelimit.slidingWindow(20, "60 s"),
        prefix: "ratelimit:api",
        ephemeralCache: cache,
      }),
      assets: new MultiRegionRatelimit({
        redis: [redis],
        limiter: MultiRegionRatelimit.slidingWindow(200, "60 s"),
        prefix: "ratelimit:assets",
        ephemeralCache: cache,
      })
    };
  }

  public async apply(request: Request): Promise<RateLimitResult> {
    const rateLimiter = this.selectRateLimiter(request);
    const userIP: string = request.headers.get("CF-Connecting-IP") || "none";
    const response = await rateLimiter.limit(userIP);

    return {
      ...response,
      retryAfterSeconds: Math.ceil((response.reset - new Date().getTime()) / 1000),
    }
  }

  private selectRateLimiter(request: Request): MultiRegionRatelimit {
    if (request.method === "POST" || request.method === "DELETE" || request.method === "PUT" || request.method === "PATCH"
      || (request.method !== "OPTIONS" && new RegExp("^/([^/]*)/repositories").test(new URL(request.url).pathname))) {
      return this.ratelimit.api;
    }
    return this.ratelimit.assets;
  }
}
