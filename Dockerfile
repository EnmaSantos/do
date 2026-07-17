FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY app ./app
COPY build ./build
COPY public ./public
COPY worker ./worker
COPY db ./db
COPY drizzle ./drizzle
COPY .openai ./.openai
COPY next.config.ts postcss.config.mjs tsconfig.json vite.config.ts ./
RUN npm run build

FROM node:24-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build /app /app
EXPOSE 3000
CMD ["npm", "run", "start", "--", "--host", "0.0.0.0"]
