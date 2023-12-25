import { defineConfig } from 'vitest/config';

export default defineConfig({
	test: {
		coverage: {
			provider: 'istanbul',
			include: ['src/**/*'],
			exclude: [
				'src/**/*.test.js',
			],
			reporter: ['cobertura', 'text'],
			all: true,
		},
	},
});
