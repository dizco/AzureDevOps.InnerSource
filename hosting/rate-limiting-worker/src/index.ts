import { RateLimiter, RateLimiterOptions } from './rateLimiter';

export interface Env {
		UPSTASH_REDIS_REST_TOKEN: string;
		UPSTASH_REDIS_REST_URL: string;
}

/**
 * It is recommended to define an ephemeral cache object outside of the handler
 * https://upstash.com/blog/cloudflare-workers-rate-limiting
 */
const redisEphemeralCache = new Map();

export default {
	async fetch(request: Request, env: Env, ctx: ExecutionContext) {
			const origins = new Map();
			origins.set("innersource.kiosoft.ca", "innersource.happysky-a6bdac0b.eastus.azurecontainerapps.io")

			const url = new URL(request.url);

			// Check if incoming hostname is a key in the ORIGINS object
			if (origins.has(url.hostname)) {
					// If it is, proxy request to that third party origin

					const rateLimiter = new RateLimiter(redisEphemeralCache, new RateLimiterOptions(env.UPSTASH_REDIS_REST_URL, env.UPSTASH_REDIS_REST_TOKEN));
					const rateLimitResponse = await rateLimiter.apply(request);
					if (rateLimitResponse.success) {
							url.hostname = origins.get(url.hostname);
							return fetch(url.toString(), request);
					}
					else {
							// show an error page for rate limited users
							return new Response(
									JSON.stringify({
											message: "You are rate limited, try again later.",
											rateLimitResponse,
									}),
									{ status: 200 }
							);
					}
			}
			// Otherwise, process request as normal
			return fetch(request);
	},
};
