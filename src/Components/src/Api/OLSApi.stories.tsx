import type { Meta, StoryObj } from "@storybook/react-vite";
import { expect, fn } from "storybook/test";
import { OLSApi } from "./OLSApi.fs.js";

const meta = {
  title: "API/OLSApi",
  render: () => <div>OLS API contract tests</div>,
} satisfies Meta;

export default meta;

type Story = StoryObj<typeof meta>;

const PARENT_IRI = "https://example.org/custom-ontology/parents/PO_0009011";

const COLLECTION = {
  id: "dataplant-id",
  label: "DataPLANT Project",
  isPublic: true,
  terminologies: [{ uri: "po", label: "po", source: "tib" }],
};

const PARENT_RESPONSE = {
  response: {
    docs: [
      {
        obo_id: "PO:0009011",
        iri: PARENT_IRI,
        ontology_name: "po",
      },
    ],
  },
};

function installFetchMock(...responseBodies: object[]) {
  const originalFetch = globalThis.fetch;
  let responseIndex = 0;
  const fetchMock = fn(async (_input: RequestInfo | URL) =>
    Response.json(responseBodies[responseIndex++]),
  );
  globalThis.fetch = fetchMock as typeof fetch;

  return {
    fetchMock,
    restore: () => {
      globalThis.fetch = originalFetch;
    },
  };
}

export const DefaultSearch: Story = {
  play: async () => {
    const identifier = "https://purl.org/nfdi4plants/ontology/dpbo/DPBO_0000033";
    const { fetchMock, restore } = installFetchMock({
      response: {
        numFound: 1,
        start: 0,
        docs: [
          {
            label: "plant age",
            obo_id: "DPBO:0000033",
            short_form: "DPBO_0000033",
            iri: identifier,
            ontology_name: "dpbo",
          },
        ],
      },
    });

    try {
      const response = await OLSApi.defaultSearch("plant age", 10, "dataplant-id");
      const url = new URL(String(fetchMock.mock.calls[0]?.[0]));

      expect(url.pathname).toBe("/api-gateway/ols/api/select");
      expect(url.searchParams.get("q")).toBe("plant age");
      expect(url.searchParams.get("rows")).toBe("10");
      expect(url.searchParams.get("collectionId")).toBe("dataplant-id");
      expect(response?.response?.docs?.[0]).toMatchObject({
        label: "plant age",
        obo_id: "DPBO:0000033",
        iri: identifier,
      });
    } finally {
      restore();
    }
  },
};

export const SearchChildrenOf: Story = {
  play: async () => {
    const { fetchMock, restore } = installFetchMock(
      PARENT_RESPONSE,
      {
        elements: [
          {
            iri: "http://purl.obolibrary.org/obo/PO_0025034",
            label: "leaf",
            shortForm: "PO_0025034",
            ontologyId: "po",
            hasDirectChildren: true,
          },
        ],
      },
    );

    try {
      const children = await OLSApi.searchChildrenOf("leaf", "PO:0009011", COLLECTION, 10);
      const parentSearchUrl = new URL(String(fetchMock.mock.calls[0]?.[0]));
      const url = new URL(String(fetchMock.mock.calls[1]?.[0]));
      const encodedParent = url.pathname.split("/").at(-2);

      expect(parentSearchUrl.pathname).toBe("/api-gateway/ols/api/select");
      expect(parentSearchUrl.searchParams.get("q")).toBe("PO:0009011");
      expect(fetchMock).toHaveBeenCalledTimes(2);
      expect(url.pathname).toContain("/ols/api/v2/ontologies/po/classes/");
      expect(decodeURIComponent(decodeURIComponent(decodeURIComponent(encodedParent!)))).toBe(PARENT_IRI);
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
      restore();
    }
  },
};

export const SearchAllChildrenOf: Story = {
  play: async () => {
    const { fetchMock, restore } = installFetchMock(
      PARENT_RESPONSE,
      {
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
      },
    );

    try {
      const children = await OLSApi.searchAllChildrenOf("PO:0009011", COLLECTION, 10);
      const url = new URL(String(fetchMock.mock.calls[1]?.[0]));

      expect(children?.map((term) => term.label)).toEqual([
        "multi-tissue plant structure",
        "leaf",
      ]);
      expect(fetchMock).toHaveBeenCalledTimes(2);
      expect(url.pathname).toContain("/children");
      expect(url.searchParams.has("search")).toBe(false);
      expect(url.searchParams.get("size")).toBe("10");
    } finally {
      restore();
    }
  },
};

export const GetCollections: Story = {
  play: async () => {
    const collections = [COLLECTION];
    const { fetchMock, restore } = installFetchMock(collections);

    try {
      const response = await OLSApi.getCollections();
      const url = new URL(String(fetchMock.mock.calls[0]?.[0]));

      expect(url.pathname).toBe("/api-gateway/collections/");
      expect(response).toEqual(collections);
    } finally {
      restore();
    }
  },
};
