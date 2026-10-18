# .NET Microservices

[![Build](https://github.com/varunkonnojutech-lab/dotnet-microservices/actions/workflows/ci.yml/badge.svg)](https://github.com/varunkonnojutech-lab/dotnet-microservices/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/varunkonnojutech-lab/dotnet-microservices)

A mini e-commerce system built with .NET 8 microservices: a **Catalog** service, an **Ordering** service, a **YARP** reverse-proxy gateway, and **RabbitMQ** for async event-driven communication. Ships with Docker Compose for local runs and Kubernetes manifests for cluster deployment.

## Who is this for

Backend developers who understand a monolith and want to see — hands-on — how services split apart, talk through a gateway, and stay decoupled with events.

## What you'll learn

- Splitting a system into independently deployable services
- API gateway routing with YARP (path-based routing to services)
- Event-driven communication with RabbitMQ (publish/subscribe, durable queues, manual ACKs)
- Keeping services decoupled: Ordering never calls Catalog directly
- Running the whole system with Docker Compose
- Deploying to Kubernetes with Deployments, Services, and a LoadBalancer

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for running/debugging outside containers) — or click **Open in GitHub Codespaces** above for a browser-based environment.
- [Docker](https://www.docker.com/) with Docker Compose
- `kubectl` + a cluster (optional, for the Kubernetes manifests)

## Quick start

```bash
git clone https://github.com/varunkonnojutech-lab/dotnet-microservices.git
cd dotnet-microservices
docker-compose up --build
```

This starts:

| Service    | Host port | Notes                              |
|------------|-----------|------------------------------------|
| Gateway    | 5000      | Reverse proxy, routes /catalog, /orders |
| Catalog    | 5001      | Product inventory API              |
| Ordering   | 5002      | Order placement API                |
| RabbitMQ   | 5672      | AMQP broker                        |
| RabbitMQ UI| 15672     | Management console (guest / guest) |

Open the RabbitMQ management UI at [http://localhost:15672](http://localhost:15672) — log in with `guest` / `guest` to watch the `order-created` queue fill as orders are placed.

## How it works

```
                    ┌─────────────────────────┐
                    │        Client           │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │     YARP Gateway        │  :5000 (compose) / :80 (k8s)
                    │  /catalog/* → catalog   │
                    │  /orders/*  → ordering  │
                    └─────┬───────────┬───────┘
                          │           │
              ┌───────────▼───┐   ┌───▼───────────┐
              │    Catalog    │   │    Ordering   │
              │  :5001 (GET)  │   │  :5002 (POST) │
              │  /products    │   │  /orders      │
              └───────▲───────┘   └───────┬───────┘
                      │                   │  publish
                      │   RabbitMQ        ▼
                      │  (order-created) ┌─────────┐
                      └──────────────────│  queue  │
                           consume       └─────────┘
```

**Event flow:**

1. A client POSTs an order to `Ordering` (via the gateway).
2. `Ordering` stores the order and publishes an `OrderCreatedEvent` (`OrderId`, `ProductId`, `Quantity`, `OccurredAt`) to the durable `order-created` queue in RabbitMQ.
3. `Catalog`'s background consumer receives the event, deserializes it, and decrements the product's stock (floored at 0, with a warning logged if stock is insufficient), then ACKs the message.

The services stay decoupled: `Ordering` never calls `Catalog` directly.

## API examples (via the gateway)

List all products:

```bash
curl http://localhost:5000/catalog/products
```

Place an order (publishes `OrderCreatedEvent`; watch stock drop on the product):

```bash
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"productId": 1, "quantity": 2}'
```

Verify the stock was decremented by the consumer:

```bash
curl http://localhost:5000/catalog/products/1
```

## Run on Kubernetes

```bash
kubectl apply -f k8s/
```

The gateway is exposed via a `LoadBalancer` service on port 80. RabbitMQ is internal (ClusterIP); the services reach it at host `rabbitmq` via cluster DNS.

> The deployment manifests use placeholder images (`ecommerce/catalog-service:latest`, etc.). Build and push your images, then update the `image:` fields before applying.

## Project structure

```
dotnet-microservices/
├── src/
│   ├── Contracts/          # Shared event contracts (OrderCreatedEvent)
│   ├── CatalogService/     # Product inventory API + RabbitMQ consumer
│   ├── OrderingService/    # Order placement API + RabbitMQ publisher
│   └── Gateway/            # YARP reverse proxy (/catalog/*, /orders/*)
├── k8s/                    # Kubernetes Deployments + Services
├── docker-compose.yml      # Local run: gateway, services, RabbitMQ
├── Microservices.sln
├── .devcontainer/          # One-click GitHub Codespaces setup
├── .github/workflows/ci.yml# CI: builds all services on push
└── LICENSE
```

## Next steps

Want the frontend to go with it? Continue with [angular-dotnet-starter](https://github.com/varunkonnojutech-lab/angular-dotnet-starter) — a full-stack Angular 18 + .NET 8 app with JWT auth.

## License

MIT — see [LICENSE](LICENSE).
