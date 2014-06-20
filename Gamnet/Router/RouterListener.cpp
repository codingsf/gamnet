/*
 * RouterListener.cpp
 *
 *  Created on: Jun 14, 2013
 *      Author: kukuta
 */

#include "RouterListener.h"
#include "RouterHandler.h"

namespace Gamnet { namespace Router {

static boost::asio::io_service& io_service_ = Singleton<boost::asio::io_service>::GetInstance();

RouterListener::RouterListener() : Network::Listener<Session>()
{
}

RouterListener::~RouterListener() {
}

void RouterListener::Init(const char* service_name, int port)
{
	RegisterHandler(
		MsgRouter_SetAddress_Req::MSG_ID,
		&RouterHandler::Recv_SetAddress_Req,
		std::shared_ptr<Network::HandlerStatic<RouterHandler>>(new Network::HandlerStatic<RouterHandler>())
	);

	RegisterHandler(
		MsgRouter_SetAddress_Ans::MSG_ID,
		&RouterHandler::Recv_SetAddress_Ans,
		std::shared_ptr<Network::HandlerStatic<RouterHandler>>(new Network::HandlerStatic<RouterHandler>())
	);

	RegisterHandler(
		MsgRouter_SetAddress_Ntf::MSG_ID,
		&RouterHandler::Recv_SetAddress_Ntf,
		std::shared_ptr<Network::HandlerStatic<RouterHandler>>(new Network::HandlerStatic<RouterHandler>())
	);

	RegisterHandler(
		MsgRouter_SendMsg_Ntf::MSG_ID,
		&RouterHandler::Recv_SendMsg_Ntf,
		std::shared_ptr<Network::HandlerStatic<RouterHandler>>(new Network::HandlerStatic<RouterHandler>())
	);
	localAddr_.service_name = service_name;
	localAddr_.cast_type = ROUTER_CAST_UNI;
	boost::asio::ip::tcp::resolver resolver_(io_service_);
	boost::asio::ip::tcp::endpoint ep_ = *resolver_.resolve({boost::asio::ip::host_name(), ""});
	localAddr_.id = ep_.address().to_v4().to_ulong();
	if(0 == localAddr_.id)
	{
		throw Exception(0, "[", __FILE__, ":", __func__, "@" , __LINE__, "] unique router id is not set");
	}
	Listener::Init(port, 4096, 0);
}

bool RouterListener::Connect(const char* host, int port, int timeout)
{
	std::shared_ptr<Session> session = sessionPool_.Create();
	if(NULL == session)
	{
		Log::Write(GAMNET_WRN, "can not create any more session(max:", 1024, ", current:", sessionManager_.Size(), ")");
		return false;
	}
	session->listener_ = this;
	session->Connect(host, port, timeout);
	return true;
}

}}
