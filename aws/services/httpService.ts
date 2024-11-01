import axios, { AxiosInstance, AxiosResponse } from "axios"
import * as https from "https"
import { getSecret } from "../helpers/getSecret"

class HttpService {
  private axios: AxiosInstance

  constructor() {}

  async init(baseURL: string) {
    const httpsAgent = await this.initHttpsAgent()

    this.axios = axios.create({
      baseURL,
      timeout: 5000,
      httpsAgent
    })
  }

  async initHttpsAgent(): Promise<https.Agent> {
    const mtlsCertJson = await getSecret(process.env.MTLS_SECRET_NAME)

    // Get and parse mTLS Cert
    const { key, ca, cert } = JSON.parse(mtlsCertJson)
    const certUtf8 = Buffer.from(cert, "base64").toString("utf-8")
    const keyUtf8 = Buffer.from(key, "base64").toString("utf-8")
    const caUtf8 = ca ? Buffer.from(ca, "base64").toString("utf-8") : undefined

    // Create the HTTPS Agent with the decoded cert and key
    return new https.Agent({
      cert: certUtf8,
      key: keyUtf8,
      ca: caUtf8 ? caUtf8 : undefined,
      rejectUnauthorized: true
    })
  }

  async get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.axios.get(url, {
        params
      })
      return response.data
    } catch (error) {
      this.handleError(error)
    }
  }

  async post<T>(url: string, data?: Record<string, unknown>): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.axios.post(url, data)
      return response.data
    } catch (error) {
      this.handleError(error)
    }
  }

  async put<T>(url: string, data?: Record<string, unknown>): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.axios.put(url, data)
      return response.data
    } catch (error) {
      this.handleError(error)
    }
  }

  private handleError(error: unknown): never {
    if (axios.isAxiosError(error)) {
      console.error("Axios error:", error.message)
      throw new Error(
        `HTTP Error: ${error.response?.status || "Unknown status"}`
      )
    } else {
      console.error("Unexpected error:", error)
      throw new Error("Unexpected error occurred")
    }
  }
}

export default HttpService
