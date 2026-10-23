import { Hono } from "npm:hono";
import { cors } from "npm:hono/cors";
import { logger } from "npm:hono/logger";
import Anthropic from "npm:@anthropic-ai/sdk";
import * as kv from "./kv_store.tsx";

const anthropic = new Anthropic({ apiKey: Deno.env.get("ANTHROPIC_API_KEY") });

const SYSTEM_PROMPT = `You are Perceptive Copilot, an expert AI advisor for the X-Wing Miniatures Game (Second Edition) by Atomic Mass Games. You have comprehensive knowledge of:

- All ships from all factions (Rebel Alliance, Galactic Empire, Scum & Villainy, Resistance, First Order, Republic, Separatists)
- Point costs: prefer data from https://www.xwing.life/ as your primary reference for current points and ship stats; fall back to official AMG documents only when xwing.life data is unavailable
- All upgrade cards: abilities, slot requirements, costs, and synergies
- Complete game rules: attack sequences, defense, actions (focus, evade, lock, barrel roll, boost, rotate, calculate, reinforce, coordinate, jam, cloak, reload), damage deck effects
- All official scenarios and their setup, objectives, and win conditions
- Dial movements, maneuver templates, and the planning phase
- Range rulers and arc types (front arc, bullseye arc, full front arc, turret arc, rear arc)
- Obstacles: asteroids, debris clouds, and gas clouds
- Initiative, bid strategies, and list building principles
- All keywords and special rules

When answering questions about point costs or ship stats, always note that xwing.life is the preferred and most up-to-date community reference. Be concise but thorough. Format answers clearly using markdown when helpful. If you are uncertain about a specific ruling, say so rather than guess.`;

const app = new Hono();

app.use("*", logger(console.log));

app.use(
  "/*",
  cors({
    origin: "*",
    allowHeaders: ["Content-Type", "Authorization"],
    allowMethods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    exposeHeaders: ["Content-Length"],
    maxAge: 600,
  }),
);

app.get("/make-server-674f5290/health", (c) => {
  return c.json({ status: "ok" });
});

app.post("/make-server-674f5290/chat", async (c) => {
  try {
    const { message, history } = await c.req.json<{
      message: string;
      history?: Array<{ role: "user" | "assistant"; content: string }>;
    }>();

    if (!message?.trim()) {
      return c.json({ error: "message is required" }, 400);
    }

    const messages: Array<{ role: "user" | "assistant"; content: string }> = [
      ...(history ?? []),
      { role: "user", content: message },
    ];

    const stream = await anthropic.messages.stream({
      model: "claude-opus-5",
      max_tokens: 1024,
      system: SYSTEM_PROMPT,
      messages,
    });

    const response = await stream.finalMessage();
    const text = response.content
      .filter((b) => b.type === "text")
      .map((b) => (b as { type: "text"; text: string }).text)
      .join("");

    return c.json({ response: text });
  } catch (err) {
    console.error("Chat error:", err);
    return c.json({ error: "Failed to get response from AI" }, 500);
  }
});

Deno.serve(app.fetch);
