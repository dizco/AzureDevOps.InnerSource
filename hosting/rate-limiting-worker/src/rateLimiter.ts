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
		public constructor(private readonly cache: Map<any, any>, private readonly options: RateLimiterOptions) {}

		public async apply(request: Request): Promise<RateLimitResult> {
				const ratelimit = new MultiRegionRatelimit({
						redis: [
								new Redis({
										url: this.options.redisUrl,
										token: this.options.redisToken,
								}),
						],
						limiter: MultiRegionRatelimit.fixedWindow(30, "60 s"),
						ephemeralCache: this.cache,
				});

				const userIP: string = request.headers.get("CF-Connecting-IP") || "none";

				const response = await ratelimit.limit(userIP);

				return {
						...response,
						retryAfterSeconds: Math.ceil((response.reset - new Date().getTime()) / 1000),
				}
		}
}
