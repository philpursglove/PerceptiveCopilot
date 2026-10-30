import { useState, useRef, useEffect } from "react";

type Role = "user" | "assistant";

interface Message {
  id: string;
  role: Role;
  content: string;
  timestamp: Date;
}

function ShipIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="w-5 h-5" xmlns="http://www.w3.org/2000/svg">
      <path d="M12 2L6 10H2L5 14H4L8 18L10 16L12 20L14 16L16 18L20 14H19L22 10H18L12 2Z" fill="currentColor" opacity="0.9"/>
    </svg>
  );
}

function TargetIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="w-4 h-4" stroke="currentColor" strokeWidth="1.5">
      <circle cx="12" cy="12" r="9"/>
      <circle cx="12" cy="12" r="4"/>
      <line x1="12" y1="2" x2="12" y2="6"/>
      <line x1="12" y1="18" x2="12" y2="22"/>
      <line x1="2" y1="12" x2="6" y2="12"/>
      <line x1="18" y1="12" x2="22" y2="12"/>
    </svg>
  );
}

function SendIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" className="w-5 h-5" stroke="currentColor" strokeWidth="2">
      <path d="M22 2L11 13" strokeLinecap="round" strokeLinejoin="round"/>
      <path d="M22 2L15 22L11 13L2 9L22 2Z" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  );
}

function TypingIndicator() {
  return (
    <div className="flex items-center gap-1 px-4 py-3">
      <div className="w-1.5 h-1.5 rounded-full bg-[#00c8ff] typing-dot"/>
      <div className="w-1.5 h-1.5 rounded-full bg-[#00c8ff] typing-dot"/>
      <div className="w-1.5 h-1.5 rounded-full bg-[#00c8ff] typing-dot"/>
    </div>
  );
}

function MessageBubble({ message }: { message: Message }) {
  const isUser = message.role === "user";

  const formattedContent = message.content
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/```([\s\S]*?)```/g, '<pre class="font-mono text-xs bg-black/40 rounded p-3 mt-2 mb-1 overflow-x-auto border border-[rgba(0,200,255,0.15)] text-[#a8d8ea]">$1</pre>')
    .replace(/`(.*?)`/g, '<code class="font-mono text-xs bg-black/30 px-1.5 py-0.5 rounded text-[#00c8ff]">$1</code>')
    .replace(/\n/g, '<br/>');

  return (
    <div className={`message-enter flex gap-3 ${isUser ? "flex-row-reverse" : "flex-row"}`}>
      <div className={`flex-shrink-0 w-8 h-8 rounded flex items-center justify-center ${
        isUser
          ? "bg-[#ff6b35]/20 text-[#ff6b35] border border-[rgba(255,107,53,0.3)]"
          : "bg-[#00c8ff]/10 text-[#00c8ff] border border-[rgba(0,200,255,0.25)]"
      }`}>
        {isUser ? <TargetIcon /> : <ShipIcon />}
      </div>

      <div className={`max-w-[78%] ${isUser ? "items-end" : "items-start"} flex flex-col gap-1`}>
        <span className="text-[10px] font-mono uppercase tracking-widest text-[#5a6e99]">
          {isUser ? "Commander" : "Perceptive Copilot"}
        </span>
        <div className={`px-4 py-3 rounded text-sm leading-relaxed ${
          isUser
            ? "bg-[#1a0d05] border border-[rgba(255,107,53,0.25)] text-[#f0d0c0]"
            : "bg-[#0a1025] border border-[rgba(0,200,255,0.15)] text-[#cdd5f0] glow-border"
        }`}
          dangerouslySetInnerHTML={{ __html: formattedContent }}
        />
        <span className="text-[10px] text-[#3a4a66] font-mono">
          {message.timestamp.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
        </span>
      </div>
    </div>
  );
}

const WELCOME_MESSAGE: Message = {
  id: "welcome",
  role: "assistant",
  content: `**Welcome to Perceptive Copilot** — your X-Wing Miniatures tactical advisor.\n\nAsk me about:\n• **Ship stats & point costs** — any ship from any faction\n• **Game rules** — attack sequences, actions, damage effects\n• **Scenarios** — setup, objectives, win conditions\n• **Upgrades** — ability text and synergy tips\n\nWhat's your first query, Commander?`,
  timestamp: new Date(),
};

export default function App() {
  const [messages, setMessages] = useState<Message[]>([WELCOME_MESSAGE]);
  const [input, setInput] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const [aiError, setAiError] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const conversationRef = useRef<Array<{ role: "user" | "assistant"; content: string }>>([]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isTyping]);

  const sendMessage = async (text: string) => {
    if (!text.trim() || isTyping) return;

    const userMsg: Message = {
      id: Date.now().toString(),
      role: "user",
      content: text.trim(),
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMsg]);
    setInput("");
    setIsTyping(true);
    setAiError(false);

    let responseText: string;
    try {
      const res = await fetch(
        "Chat/chat",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            message: text.trim(),
            history: conversationRef.current,
          }),
        }
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      responseText = data.response;
      conversationRef.current = [
        ...conversationRef.current,
        { role: "user", content: text.trim() },
        { role: "assistant", content: responseText },
      ];
    } catch {
      setAiError(true);
      responseText = '';
    }

    const assistantMsg: Message = {
      id: (Date.now() + 1).toString(),
      role: "assistant",
      content: responseText,
      timestamp: new Date(),
    };

    setIsTyping(false);
    setMessages((prev) => [...prev, assistantMsg]);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    sendMessage(input);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage(input);
    }
  };

  return (
    <div className="size-full flex flex-col bg-[#07091a] overflow-hidden relative">
      {/* Scanline overlay */}
      <div className="scanline absolute inset-0 pointer-events-none z-10"/>

      {/* Starfield */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        {Array.from({ length: 80 }).map((_, i) => (
          <div
            key={i}
            className="absolute rounded-full bg-white"
            style={{
              width: Math.random() > 0.9 ? 2 : 1,
              height: Math.random() > 0.9 ? 2 : 1,
              top: `${Math.random() * 100}%`,
              left: `${Math.random() * 100}%`,
              opacity: 0.1 + Math.random() * 0.4,
            }}
          />
        ))}
      </div>

      <div className="relative z-20 flex flex-col size-full max-w-4xl mx-auto w-full">
        {/* Header */}
        <header className="flex-shrink-0 px-6 py-4 border-b border-[rgba(0,200,255,0.1)] flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded flex items-center justify-center bg-[#00c8ff]/10 border border-[rgba(0,200,255,0.3)] text-[#00c8ff] glow-border">
              <ShipIcon />
            </div>
            <div>
              <h1 className="font-['Exo_2'] font-bold text-base tracking-wide text-[#e2e8f8] glow-text">
                PERCEPTIVE COPILOT
              </h1>
              <p className="text-[10px] font-mono tracking-widest text-[#5a6e99] uppercase">
                X-Wing Tactical Advisor · v2.6
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-[#00c8ff]" style={{ animation: "pulse-glow 2s ease-in-out infinite" }}/>
            <span className="text-[10px] font-mono text-[#5a6e99] uppercase tracking-widest">Online</span>
          </div>
        </header>


        {/* Messages */}
        <div className="flex-1 overflow-y-auto px-4 md:px-6 py-6 flex flex-col gap-5">
          {messages.map((msg) => (
            <MessageBubble key={msg.id} message={msg} />
          ))}
          {isTyping && (
            <div className="message-enter flex gap-3">
              <div className="flex-shrink-0 w-8 h-8 rounded flex items-center justify-center bg-[#00c8ff]/10 text-[#00c8ff] border border-[rgba(0,200,255,0.25)] glow-border">
                <ShipIcon />
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-[10px] font-mono uppercase tracking-widest text-[#5a6e99]">Perceptive Copilot</span>
                <div className="px-4 py-1 rounded bg-[#0a1025] border border-[rgba(0,200,255,0.15)] glow-border">
                  <TypingIndicator />
                </div>
              </div>
            </div>
          )}
          <div ref={messagesEndRef}/>
        </div>

        {/* Input */}
        <div className="flex-shrink-0 px-4 md:px-6 py-4 border-t border-[rgba(0,200,255,0.1)]">
          <form onSubmit={handleSubmit} className="flex gap-3 items-end">
            <div className="flex-1 relative">
              <textarea
                ref={inputRef}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Ask about rules, ship costs, scenarios..."
                rows={1}
                className="w-full bg-[#0d1130] border border-[rgba(0,200,255,0.2)] rounded px-4 py-3 text-sm text-[#e2e8f8] placeholder-[#3a4a66] resize-none focus:outline-none focus:border-[rgba(0,200,255,0.5)] focus:shadow-[0_0_0_1px_rgba(0,200,255,0.2)] transition-all duration-150 font-['Inter'] leading-relaxed"
                style={{ minHeight: "48px", maxHeight: "120px" }}
                onInput={(e) => {
                  const el = e.currentTarget;
                  el.style.height = "auto";
                  el.style.height = Math.min(el.scrollHeight, 120) + "px";
                }}
              />
            </div>
            <button
              type="submit"
              disabled={!input.trim() || isTyping}
              className="flex-shrink-0 w-12 h-12 rounded flex items-center justify-center bg-[#00c8ff]/10 border border-[rgba(0,200,255,0.3)] text-[#00c8ff] hover:bg-[#00c8ff]/20 hover:border-[rgba(0,200,255,0.6)] transition-all duration-150 disabled:opacity-30 disabled:cursor-not-allowed glow-border"
            >
              <SendIcon />
            </button>
          </form>
          <p className="mt-2 text-[10px] font-mono text-center" style={{ color: aiError ? "#ff6b35" : "#3a4a66" }}>
            {aiError
              ? "AI connection failed — showing demo answers"
              : "Enter to send · Shift+Enter for new line · Powered by Claude"}
          </p>
        </div>
      </div>
    </div>
  );
}
