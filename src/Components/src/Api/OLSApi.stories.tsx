import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fn } from "storybook/test";
import { OLSApi } from "./OLSApi.fs.js";

const meta = {
  title: "API/OLSApi",
  render: () => <div>OLS API contract tests</div>,
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

export const CanonicalizesEntityIdentifier: Story = {
  play: async () => {
    const identifier = "http://purl.org/ontology/mo/Instrument";
    const entityType = "http://www.w3.org/2002/07/owl#Class";
    const semanticArtefactType = "https://w3id.org/mod#SemanticArtefact";
    const originalFetch = globalThis.fetch;

    globalThis.fetch = fn(
      async () =>
        new Response(
          JSON.stringify({
            _embedded: {
              terms: [
                {
                  iri: identifier,
                  URI: identifier,
                  type: entityType,
                  "@type": semanticArtefactType,
                },
              ],
            },
          }),
          {
            status: 200,
            headers: { "Content-Type": "application/json" },
          },
        ),
    ) as unknown as typeof fetch;

    try {
      const response = await OLSApi.getTermByIRI("mo", identifier);
      const entity = response._embedded?.terms?.[0] as Record<string, unknown>;

      expect(entity).toHaveProperty("iri", identifier);
      expect(entity).not.toHaveProperty("URI");
      expect(entity.iri).toBe(identifier);
      expect(entity.type).toBe(entityType);
      expect(entity["@type"]).toBe(semanticArtefactType);
    } finally {
      globalThis.fetch = originalFetch;
    }
  },
};
