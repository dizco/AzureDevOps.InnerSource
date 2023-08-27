import { MultiRegionRatelimit } from '@upstash/ratelimit';
import { Redis } from '@upstash/redis/types/platforms/cloudflare';

export interface RateLimitResult {
		success: boolean;
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
						limiter: MultiRegionRatelimit.fixedWindow(5, "5 s"),
						ephemeralCache: this.cache,
				});

				const userIP: string = request.headers.get("CF-Connecting-IP") || "none";

				return await ratelimit.limit(userIP);
		}
}
