export default {
	async fetch(request: Request) {
			const origins = new Map();
			origins.set("innersource.kiosoft.ca", "innersource.happysky-a6bdac0b.eastus.azurecontainerapps.io")

			const url = new URL(request.url);

			// Check if incoming hostname is a key in the ORIGINS object
			if (origins.has(url.hostname)) {
					url.hostname = origins.get(url.hostname);
					// If it is, proxy request to that third party origin
					return fetch(url.toString(), request);
			}
			// Otherwise, process request as normal
			return fetch(request);
	},
};
