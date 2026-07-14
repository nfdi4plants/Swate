import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fn } from "storybook/test";
import { OLSApi } from "./OLSApi.fs.js";
import { Swate_Components_Api_OLSApi_OLSTypes_SearchApi__SearchApi_ToSwateTerms as toTerms } from "../Composite/TermSearch/Types.fs.js";

const meta = {
  title: "API/OLSApi",
  render: () => <div>OLS API contract tests</div>,
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

export const SearchesACollection: Story = {
  play: async () => {
    const originalFetch = globalThis.fetch;
    const identifier = "https://purl.org/nfdi4plants/ontology/dpbo/DPBO_0000033";
    let requestedUrl = "";

    globalThis.fetch = fn(async (input: RequestInfo | URL) => {
      requestedUrl = String(input);

      return Response.json({
        response: {
          numFound: 1,
          start: 0,
          docs: [
            {
              label: "plant age",
              short_form: "DPBO_0000033",
              iri: identifier,
              URI: identifier,
              ontology_name: "dpbo",
              type: "class",
              "@type": "https://w3id.org/mod#SemanticArtefact",
            },
          ],
        },
      });
    }) as typeof fetch;

    try {
      const response = await OLSApi.defaultSearch(
        "plant age",
        10,
        "dataplant-id",
      );
      const url = new URL(requestedUrl);

      expect(url.pathname).toBe("/api-gateway/ols/api/select");
      expect(url.searchParams.get("q")).toBe("plant age");
      expect(url.searchParams.get("rows")).toBe("10");
      expect(url.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(response?.response?.docs?.[0]?.label).toBe("plant age");

      if (!response) {
        throw new Error("Expected an OLS search response.");
      }

      const entity = response.response?.docs?.[0];
      expect(entity).toHaveProperty("iri", identifier);
      expect(entity).toHaveProperty("URI", identifier);
      expect(entity).toHaveProperty("type", "class");
      expect(entity).toHaveProperty("@type", "https://w3id.org/mod#SemanticArtefact");

      const terms = toTerms(response);
      expect(terms).toHaveLength(1);
      expect(terms[0]).toMatchObject({
        name: "plant age",
        id: "DPBO:0000033",
        source: "dpbo",
        href: identifier,
      });
    } finally {
      globalThis.fetch = originalFetch;
    }
  },
};

export const SearchesDirectChildren: Story = {
  play: async () => {
    const originalFetch = globalThis.fetch;
    let requestedUrl = "";

    globalThis.fetch = fn(async (input: RequestInfo | URL) => {
      requestedUrl = String(input);

      return Response.json({
        elements: [
          {
            iri: "http://purl.obolibrary.org/obo/PO_0025034",
            label: "leaf",
            shortForm: "PO_0025034",
            ontologyId: "po",
            hasDirectChildren: true,
          },
        ],
      });
    }) as typeof fetch;

    try {
      const collection = {
        id: "dataplant-id",
        label: "DataPLANT Project",
        isPublic: true,
        terminologies: [{ uri: "po", label: "po", source: "tib" }],
      };
      const children = await OLSApi.searchChildrenOf(
        "leaf",
        "PO:0009011",
        collection,
        10,
      );
      const url = new URL(requestedUrl);
      const encodedParent = url.pathname.split("/").at(-2);

      expect(url.pathname).toContain("/ols/api/v2/ontologies/po/classes/");
      expect(decodeURIComponent(decodeURIComponent(decodeURIComponent(encodedParent!)))).toBe(
        "http://purl.obolibrary.org/obo/PO_0009011",
      );
      expect(url.searchParams.get("database")).toBe("tib");
      expect(url.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(url.searchParams.get("search")).toBe("leaf");
      expect(url.searchParams.get("size")).toBe("10");
      expect(children).toHaveLength(1);
      expect(children?.[0]).toMatchObject({
        label: "leaf",
        iri: "http://purl.obolibrary.org/obo/PO_0025034",
        shortForm: "PO_0025034",
        ontologyId: "po",
      });
    } finally {
      globalThis.fetch = originalFetch;
    }
  },
};

export const SearchesAllChildren: Story = {
  play: async () => {
    const originalFetch = globalThis.fetch;
    const fetchMock = fn(async (_input: RequestInfo | URL) =>
      Response.json({
        elements: [
          {
            iri: "http://purl.obolibrary.org/obo/PO_0025496",
            label: "multi-tissue plant structure",
            shortForm: "PO_0025496",
            ontologyId: "po",
          },
          {
            iri: "http://purl.obolibrary.org/obo/PO_0025034",
            label: "leaf",
            shortForm: "PO_0025034",
            ontologyId: "po",
          },
        ],
      }),
    );

    globalThis.fetch = fetchMock as typeof fetch;

    try {
      const collection = {
        id: "dataplant-id",
        label: "DataPLANT Project",
        isPublic: true,
        terminologies: [{ uri: "po", label: "po", source: "tib" }],
      };
      const children = await OLSApi.searchAllChildrenOf("PO:0009011", collection, 10);
      const url = new URL(String(fetchMock.mock.calls[0]?.[0]));

      expect(children?.map((term) => term.label)).toEqual([
        "multi-tissue plant structure",
        "leaf",
      ]);
      expect(fetchMock).toHaveBeenCalledTimes(1);
      expect(url.pathname).toContain("/children");
      expect(url.searchParams.has("search")).toBe(false);
      expect(url.searchParams.get("size")).toBe("10");
    } finally {
      globalThis.fetch = originalFetch;
    }
  },
};
